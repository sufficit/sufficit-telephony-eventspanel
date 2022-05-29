using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class ChannelInfoMonitor : EventsPanelMonitor<ChannelInfo>
    {
        public ChannelInfoMonitor(string key) : base(new ChannelInfo(key)) { }

        #region IMPLEMENT ABSTRACT MONITOR CONTENT

        public override void Event(object @event)
        {
            if (@event is IChannelEvent channelEvent)
            {
                bool Updated = false;

                if (channelEvent is IManagerEvent managerEvent)
                    if(UpdateReceived(this, managerEvent.GetTimeStamp()))
                        Updated = true;

                // if this event is newer, check state and extra info
                if (channelEvent is IChannelInfoEvent channelInfoEvent)
                    if (HandleChannelInfo(this.Content, channelInfoEvent))
                        Updated = true;

                if (channelEvent is HangupEvent hangupEvent)
                {
                    if (HandleHangup(this.Content, hangupEvent))
                        Updated = true;
                } 
                else if(channelEvent is NewChannelEvent newChannelEvent)
                {
                    if (HandleNewChannel(this.Content, newChannelEvent))
                        Updated = true;
                }

                if (Updated)                    
                    base.Event(@event);
            }
        }

        #endregion

        public static bool HandleChannelInfo(ChannelInfo content, IChannelInfoEvent @event)
        {
            bool updated = false;

            if (string.IsNullOrWhiteSpace(content.UniqueId))
            {
                content.UniqueId = @event.UniqueId;
                updated = true;
            }

            if (string.IsNullOrWhiteSpace(content.LinkedId))
            {
                content.LinkedId = @event.LinkedId;
                updated = true;
            }

            if (content.State != @event.ChannelState)
            {
                content.State = @event.ChannelState;
                updated = true;
            }

            content.Exten = @event.Exten;
            content.CallerIdNum = @event.CallerIdNum;
            content.CallerIdName = @event.CallerIdName;
            content.ConnectedLineNum = @event.ConnectedLineNum;
            content.ConnectedLineName = @event.ConnectedLineName;

            return updated;
        }

        /// <summary>
        /// This monitor has initiated the action ? <br />
        /// If peer, is outbound call ? <br />
        /// If trunk, is inbound call ?
        /// </summary>
        public bool IsInitiator => Content.LinkedId == Content.UniqueId;

        protected static bool UpdateReceived(ChannelInfo content, DateTime dateTime)
        {
            if (dateTime > DateTime.MinValue)
            {
                if (content.Start == DateTime.MinValue || dateTime < content.Start)
                {
                    content.Start = dateTime;
                    return true;
                }
            }

            return false;
        }

        public static bool HandleHangup(ChannelInfo content, HangupEvent @event)
        {
            if (content.Hangup == null)
            {
                content.Hangup = new Hangup();
                content.Hangup.Code = @event.Cause;
                content.Hangup.Description = @event.CauseTxt;
                content.Hangup.Timestamp = @event.GetTimeStamp();
                return true;
            }
            return false;
        }

        public static bool HandleNewChannel(ChannelInfo content, NewChannelEvent @event)
        {
            content.DialedExten = @event.Exten;    
            return true;
        }

        public string GetChannelLabel(EventsPanelCardKind kind)
        {
            string result = Content.Exten ?? string.Empty;

            if (kind == EventsPanelCardKind.TRUNK)
            {
                if (IsInitiator)
                {
                    if (Utils.TryFormatToE164(Content.DialedExten, out string did))
                        Content.DirectInwardDialing = did;
                }
                else
                {
                    // callerid defined by trunk options
                    if (Utils.TryFormatToE164(Content.CallerIdNum, out string callerid))
                        Content.OutboundCallerId = callerid;

                    // callerid defined by extension
                    if (Utils.IsValidPhoneNumber(Content.ConnectedLineNum, false))
                        Content.OutboundCallerId = Content.ConnectedLineNum ?? string.Empty;
                }
            }

            if (string.IsNullOrWhiteSpace(result))
            {
                if (kind == EventsPanelCardKind.TRUNK && !string.IsNullOrWhiteSpace(Content.CallerIdNum) && Utils.IsValidPhoneNumber(Content.CallerIdNum, false))
                {
                    result = Content.CallerIdNum;
                }
                else if (kind == EventsPanelCardKind.PEER && !string.IsNullOrWhiteSpace(Content.DialedExten) && Utils.IsValidPhoneNumber(Content.DialedExten, true))
                {
                    result = Content.DialedExten;
                }
                else
                {
                    if (ValidCallerId(Content.ConnectedLineNum))
                    {
                        result = Content.ConnectedLineNum!;
                    }
                    else if (ValidCallerId(Content.Exten))
                    {
                        result = Content.Exten!;
                    }
                }
            }

            if (Utils.IsValidPhoneNumber(result, true))
                return Utils.FormatToE164Semantic(result);
            else return Key;
        }

        public static bool ValidCallerId(string? callerId)
        {
            if (!string.IsNullOrWhiteSpace(callerId))
            {
                if(callerId != "<unknown>")
                {
                    return true;
                }
            }
            return false;
        }
    }
}
