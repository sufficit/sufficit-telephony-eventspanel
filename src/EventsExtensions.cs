using Sufficit.Asterisk;
using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public static class EventsExtensions
    {
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp);
            return dateTime;
        }

        /// <summary>
        /// Recover a real timestamp of an event or the time of manager proxy received that <br />
        /// Universal time
        /// </summary>
        /// <returns></returns>
        public static DateTime GetTimeStamp(this IManagerEvent source)
        {
            if(source is ManagerEventFromAsterisk asterisk)
            {
                var timestamp = asterisk.Timestamp;
                if (timestamp != null && timestamp.Value > 0)
                    return UnixTimeStampToDateTime(timestamp.Value);                
            }

            return source.DateReceived;
        }

        public static EventsPanelCardMonitor ToCard(this IManagerEvent source, EventsPanelService service)
        {
            switch (source)
            {
                case PeerEntryEvent     mEvent: return ToCard(mEvent, service);
                case PeerStatusEvent    mEvent: return ToCard(mEvent, service);
                case IChannelEvent      mEvent: return ToCard(mEvent, service);
                case IQueueEvent        mEvent: return ToCard(mEvent, service);
                default: throw new NotImplementedException();
            }
        }

        public static EventsPanelCardMonitor ToCard(this IQueueEvent source, EventsPanelService service)
        {
            var card = new EventsPanelQueueCard();
            card.Label = source.Queue;
            return card.CardMonitor(service);
        }
        
        public static EventsPanelCardMonitor ToCard(this PeerStatusEvent source, EventsPanelService service)
        {
            var peerId = source.Peer;
            var card = new EventsPanelCard();
            card.Key = peerId;
            card.Label = peerId;
            card.Channels.Add($"^{ peerId }");

            if (card.Label.Contains('/'))
            {
                var splitted = card.Label.Split('/');
                card.Label = splitted[1];
            }

            return card.CardMonitor(service);
        }

        public static EventsPanelCardMonitor ToCard(this IChannelEvent source, EventsPanelService service)
        {
            var channel = new AsteriskChannel(source.Channel);
            var peerId = channel.GetPeer();
            var card = new EventsPanelCard();
            card.Label = channel.Name;
            card.Channels.Add($"^{ peerId }");

            return card.CardMonitor(service);
        }

        public static EventsPanelCardMonitor ToCard(this PeerEntryEvent source, EventsPanelService service)
        {
            var info = new Asterisk.PeerInfo();
            info.Protocol = source.ChannelType;
            info.Name = source.ObjectName;
            
            var channel = new AsteriskChannel(info);
            var peerId = channel.GetPeer();
            var card = new EventsPanelCard();
            card.Label = channel.Name;
            card.Channels.Add($"^{ peerId }");

            if (!string.IsNullOrWhiteSpace(source.Description))
                card.Label = source.Description;

            return card.CardMonitor(service);
        }

        public static void Event(this EventsPanelService source, EventsPanelCardMonitor monitor, IManagerEventFromAsterisk @event)
        {
            var timestamp = @event.GetTimeStamp();
            if (monitor.MaxUpdate < timestamp)
            {
                monitor.MaxUpdate = timestamp;
            }        
            else if(monitor.MinUpdate > timestamp || monitor.MinUpdate == DateTime.MinValue)
            {
                monitor.MinUpdate = timestamp;                
            }

            if (@event is IChannelEvent eventChannel)
                monitor.IChannelEvent(source.Channels, eventChannel);

            if (@event is IPeerStatusEvent eventPeerStatus)
                monitor.IPeerStatusEvent(eventPeerStatus);

            if (@event is IQueueEvent eventQueue)
                monitor.IQueueEvent(eventQueue);

            switch (@event)
            {
                case PeerEntryEvent newEvent: monitor.Event(newEvent); break;
                default: break;
            }            
        }
        
        public static void IChannelEvent(this EventsPanelCardMonitor source, ChannelInfoCollection channels, IChannelEvent @event)
        {
            var channelId = @event.Channel;
            var channel = source.Channels[channelId];
            if (channel == null)
            {
                channel = channels[channelId];
                if (channel == null)
                    throw new InvalidOperationException("null channel on card");
                
                source.Channels.Add(channel);
                source.Event(@event);
            }
        }

        public static void IPeerStatusEvent(this EventsPanelCardMonitor source, IPeerStatusEvent @event)
        {
            PeerInfo content = source.Content<PeerInfoMonitor>()!;                
            if (content.Status != @event.PeerStatus
                || content.Address != @event.Address
                || content.Cause != @event.Cause
                || content.Time != @event.Time
                )
            {
                content.Status = @event.PeerStatus;
                content.Address = @event.Address;
                content.Cause = @event.Cause;
                content.Time = @event.Time;

                source.Event(@event);
            }            
        }

        public static void IQueueEvent(this EventsPanelCardMonitor source, IQueueEvent @event)
        { 
            source.Event(@event);            
        }

        /*
        public static void Event(this EventsPanelCardMonitor source, PeerEntryEvent @event)
        {
            var currentState = @event.GetPeerStatus();
            var content = source.Content<PeerInfo>();
            if (content.Status != currentState)
            {
                content.Status = currentState;
                source.Event(@event);
            }
        }
        */
    }
}
