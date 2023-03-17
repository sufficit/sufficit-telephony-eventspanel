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

        public static EventsPanelCardKind GetKindByKey(string key)
        {
            if (!key.Contains('/'))
            {
                return EventsPanelCardKind.QUEUE;
            }
            return EventsPanelCardKind.PEER;
        }


        public static EventsPanelCard ToCard(this IManagerEvent source, string key, EventsPanelService service)
        {
            switch (GetKindByKey(key))
            {
                case EventsPanelCardKind.QUEUE: return HandleQueueCard(key, service);
                case EventsPanelCardKind.PEER: return HandlePeerCard(key, service);
                default: throw new NotImplementedException($"{source.GetType()} to card not implemented yet");
            }

            /*
            switch (source)
            {
                case IPeerStatus        mEvent: return HandleCardByEvent(mEvent, service);
                case IChannelEvent      mEvent: return HandleCardByEvent(mEvent, service);
                case IQueueEvent        mEvent: return HandleCardByEvent(mEvent, service);
                default: throw new NotImplementedException($"{ source.GetType() } to card not implemented yet");
            }
            */
        }

        public static EventsPanelCard HandleQueueCard(string key, EventsPanelService service)
        {
            var cardinfo = new EventsPanelCardInfo();
            cardinfo.Kind = EventsPanelCardKind.QUEUE;
            cardinfo.Label = key;

            return cardinfo.CardCreate(service);
        }

        public static EventsPanelCard HandlePeerCard(string key, EventsPanelService service)
        {
            var channel = new AsteriskChannel(key);
            var cardinfo = new EventsPanelCardInfo();
            cardinfo.Kind = EventsPanelCardKind.PEER;
            cardinfo.Label = channel.Name ?? "Unlabeled";
            cardinfo.Channels.Add($"^{key}");

            return cardinfo.CardCreate(service);
        }


        public static EventsPanelCard HandleCardByEvent(this IQueueEvent source, EventsPanelService service)
        {
            var cardinfo = new EventsPanelCardInfo();
            cardinfo.Kind = EventsPanelCardKind.QUEUE;
            cardinfo.Label = source.Queue;

            return cardinfo.CardCreate(service);
        }
        
        public static EventsPanelCard HandleCardByEvent(this IPeerStatus source, EventsPanelService service)
        {
            var peerId = source.Peer;
            var cardinfo = new EventsPanelCardInfo();
            cardinfo.Label = peerId;
            cardinfo.Channels.Add($"^{ peerId }");

            if (cardinfo.Label.Contains('/'))
            {
                var splitted = cardinfo.Label.Split('/');
                cardinfo.Label = splitted[1];
            }

            return cardinfo.CardCreate(service);
        }

        public static EventsPanelCard HandleCardByEvent(this IChannelEvent source, EventsPanelService service)
        {
            var channel = new AsteriskChannel(source.Channel);
            var peerId = channel.GetPeer();
            Console.WriteLine($"adding a new peer card, key: {peerId}");

            var cardinfo = new EventsPanelCardInfo();            
            cardinfo.Kind = EventsPanelCardKind.PEER;
            cardinfo.Label = channel.Name ?? "Unlabeled";
            cardinfo.Channels.Add($"^{ peerId }");

            return cardinfo.CardCreate(service);
        }

        public static Task EventAsync(this EventsPanelService source, EventsPanelCard card, IManagerEventFromAsterisk @event) 
            => Task.Factory.StartNew(() => Event(source,card, @event));

        /// <summary>
        /// Where card exists
        /// </summary>
        /// <param name="source"></param>
        /// <param name="card"></param>
        /// <param name="event"></param>
        public static void Event(this EventsPanelService source, EventsPanelCard card, IManagerEventFromAsterisk @event)
        {
            if (@event is IChannelEvent eventChannel)
                card.IChannelEvent(source.Channels, eventChannel);

            if (@event is IPeerStatusEvent eventPeerStatus)
                card.IPeerStatusEvent(eventPeerStatus);

            if (@event is IQueueEvent eventQueue)
                card.IQueueEvent(eventQueue);

            switch (@event)
            {
                case PeerEntryEvent newEvent: card.Monitor?.Event(newEvent); break;
                default: break;
            }            
        }
        
        public static void IChannelEvent(this EventsPanelCard source, ChannelInfoCollection channels, IChannelEvent @event)
        {
            var channelId = @event.Channel;
            var monitor = source.Channels[channelId];
            if (monitor == null)
            {
                monitor = channels[channelId];
                if (monitor == null)
                    throw new InvalidOperationException($"null channel monitor on card: { channelId }");
                
                source.Channels.Add(monitor);
                source.Monitor?.Event(@event);
            }
        }

        public static void IPeerStatusEvent(this EventsPanelCard source, IPeerStatusEvent @event)
        {
            if (source.IsMonitored)
            {
                PeerInfo? content = source.Content<PeerInfo?>();
                if (content != null && 
                    ( content.Status != @event.PeerStatus
                    || content.Address != @event.Address
                    || content.Cause != @event.Cause
                    || content.Time != @event.Time
                    ))
                {
                    content.Status = @event.PeerStatus;
                    content.Address = @event.Address;
                    content.Cause = @event.Cause;
                    content.Time = @event.Time;

                    source.Monitor?.Event(@event);
                }
            }
        }

        public static void IQueueEvent(this EventsPanelCard source, IQueueEvent @event)
        { 
            source.Monitor?.Event(@event);            
        }

        /*
        public static void Event(this EventsPanelCard source, PeerEntryEvent @event)
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
