using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public partial class EventsPanelService
    {
        public static T Monitor<T>(GenericCollection<T> collection, string key) where T : IMonitor
        {
            IMonitor? monitor = collection[key];
            if (monitor == null)
            {
                switch (collection)
                {
                    case GenericCollection<ChannelInfoMonitor>: monitor = new ChannelInfoMonitor(key); break;
                    case GenericCollection<PeerInfoMonitor>: monitor = new PeerInfoMonitor(key); break;
                    case GenericCollection<QueueInfoMonitor>: monitor = new QueueInfoMonitor(key); break;
                    default: throw new ArgumentException("invalid type of imonitor");
                }
                
                collection.Add((T)monitor);
            }
            return (T)monitor;
        }


        /// <summary>
        /// Handle Channel events and create a monitor if not exists
        /// </summary>
        /// <param name="source"></param>
        /// <param name="event"></param>
        /// <returns></returns>
        public static string HandleEvent(EventsPanelService source, IChannelEvent @event)
        {
            var key = @event.Channel;
            var monitor = Monitor(source.Channels, key);
            monitor.Event(@event);
            return key;
        }

        /// <summary>
        /// Handle Peer events and create a monitor if not exists
        /// </summary>
        /// <param name="source"></param>
        /// <param name="event"></param>
        /// <returns></returns>
        public static string HandleEvent(EventsPanelService source, IPeerStatus @event)
        {
            var key = @event.Peer;
            var monitor = Monitor(source.Peers, @event.Peer);
            monitor.Event(@event);
            return key;
        }

        /// <summary>
        /// Handle Queue events and create a monitor if not exists
        /// </summary>
        /// <param name="source"></param>
        /// <param name="event"></param>
        /// <returns></returns>
        public static string HandleEvent(EventsPanelService source, IQueueEvent @event)
        {
            var key = @event.Queue;
            var monitor = Monitor(source.Queues, @event.Queue);
            monitor.Event(@event);
            return key;
        }
    }
}
