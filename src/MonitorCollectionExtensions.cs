using Sufficit.Asterisk.Manager.Events;
using Sufficit.Asterisk.Manager.Events.Abstracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public static class MonitorCollectionExtensions
    {
        /// <summary>
        ///     Handle Channel events and create a monitor if not exists
        /// </summary>
        public static string HandleEvent(this ChannelInfoCollection source, IChannelEvent @event)
        {
            string? queue = null;
            if (@event is IQueueEvent eventQueue)
                queue = eventQueue.Queue;

            // send channel to monitor
            var monitor = source.Monitor(@event.Channel, queue);
            monitor.Event(@event);

            // return discover key to card
            var index = @event.Channel.LastIndexOf('-');
            var key = @event.Channel.Substring(0, index);
            return key;
        }


        /// <summary>
        /// Handle Peer events and create a monitor if not exists
        /// </summary>
        public static string HandleEvent(this PeerInfoCollection source, SecurityEvent @event)
        {
            var key = $"{@event.Service}/{@event.AccountId}";
            var monitor = source.Monitor(key);
            monitor.Event(@event);
            return key;
        }

        /// <summary>
        /// Handle Peer events and create a monitor if not exists
        /// </summary>
        public static string HandleEvent(this PeerInfoCollection source, IPeerStatus @event)
        {
            var key = @event.Peer;
            var monitor = source.Monitor(key);
            monitor.Event(@event);
            return key;
        }

        /// <summary>
        /// Handle Queue events and create a monitor if not exists
        /// </summary>
        /// <param name="source"></param>
        /// <param name="event"></param>
        /// <returns></returns>
        public static string HandleEvent(this QueueInfoCollection source, IQueueEvent @event)
        {
            var key = @event.Queue;
            var monitor = source.Monitor(@event.Queue);
            monitor.Event(@event);
            return key;
        }
    }
}
