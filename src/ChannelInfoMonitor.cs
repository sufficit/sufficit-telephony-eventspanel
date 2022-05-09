using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Linq;
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
                switch (channelEvent)
                {
                    case StatusEvent newEvent:          Handle(this, newEvent, out Updated); break;
                    case NewChannelEvent newEvent:      Handle(this, newEvent, out Updated); break;
                    case NewStateEvent newEvent:        Handle(this, newEvent, out Updated); break;
                    case HangupEvent newEvent:          Handle(this, newEvent, out Updated); break;
                }

                if (Updated)                    
                    base.Event(@event);
            }
        }

        #endregion

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

        public static void Handle(ChannelInfoMonitor source, StatusEvent @event, out bool updated)
        {
            updated = UpdateReceived(source, @event.GetTimeStamp());
            var content = source.GetContent();

            // if this event is newer, check state and extra info
            if (updated) HandleState(content, @event);
        }

        public static void Handle(ChannelInfoMonitor source, NewChannelEvent @event, out bool updated)
        {
            updated = UpdateReceived(source, @event.GetTimeStamp());
            var content = source.GetContent();

            // if this event is newer, check state and extra info
            if (updated) HandleState(content, @event);
        }

        public static void Handle(ChannelInfoMonitor source, NewStateEvent @event, out bool updated)
        {
            updated = UpdateReceived(source, @event.GetTimeStamp());
            var content = source.GetContent();

            // if this event is newer, check state and extra info
            if (updated) HandleState(content, @event);
        }

        public static void Handle(ChannelInfoMonitor source, HangupEvent @event, out bool updated)
        {
            updated = UpdateReceived(source, @event.GetTimeStamp());
            var content = source.GetContent();

            // if this event is newer, check state and extra info
            if (updated) HandleState(content, @event);            

            if (content.Hangup == null)
            {
                content.Hangup = new Hangup();
                content.Hangup.Code = @event.Cause;
                content.Hangup.Description = @event.CauseTxt;
                content.Hangup.Timestamp = @event.DateReceived;
                updated = true;
            }            
        }

        public static void HandleState(ChannelInfo content, IChannelInfoEvent @event)
        {
            content.State = @event.ChannelState;  
            content.CallerIDNum = @event.CallerIdNum;
            content.CallerIDName = @event.CallerIdName;
        }
    }
}
