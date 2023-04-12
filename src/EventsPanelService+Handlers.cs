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
        public static string GetKeyFromEvent(object @event)
        {
            switch (@event)
            {
                case NewStateEvent @new: 
                    {
                        var index = @new.Channel.LastIndexOf('-');
                        return @new.Channel.Substring(0, index);
                    }
                case NewChannelEvent @new:
                    {
                        var index = @new.Channel.LastIndexOf('-');
                        return @new.Channel.Substring(0, index);
                    }
                case HangupEvent @new:
                    {
                        var index = @new.Channel.LastIndexOf('-');
                        return @new.Channel.Substring(0, index);
                    }
                case IChannelEvent @new: return @new.Channel;
                default: return "invalid";
            }
        }

        /// <summary>
        /// GetOrCreate Monitor
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static T Monitor<T>(MonitorCollection<T> collection, string key) where T : IMonitor
        {
            IMonitor? monitor = collection[key];
            if (monitor == null)
            {
                monitor = collection switch
                {
                    MonitorCollection<PeerInfoMonitor> => new PeerInfoMonitor(key),
                    MonitorCollection<QueueInfoMonitor> => new QueueInfoMonitor(key),
                    _ => throw new ArgumentException("invalid type of imonitor"),
                };
                collection.Add((T)monitor);
            }
            return (T)monitor;
        }

        /// <summary>
        /// GetOrCreate Monitor
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static ChannelInfoMonitor Monitor(MonitorCollection<ChannelInfoMonitor> collection, string key, string? queue = null)
        {
            ChannelInfoMonitor? monitor = collection[key];
            if (monitor == null)
            {
                monitor = new ChannelInfoMonitor(key);
                monitor.Content.Queue = queue;
                collection.Add(monitor);
            }
            return monitor;
        }

        /// <summary>
        /// Handle Channel events and create a monitor if not exists
        /// </summary>
        /// <param name="source"></param>
        /// <param name="event"></param>
        /// <returns></returns>
        public static string HandleEvent(EventsPanelService source, IChannelEvent @event)
        {
            string? queue = null;
            if(@event is IQueueEvent eventQueue)
                queue = eventQueue.Queue;            

            // send channel to monitor
            var monitor = Monitor(source.Channels, @event.Channel, queue);
            monitor.Event(@event);

            // return discover key to card
            var index = @event.Channel.LastIndexOf('-');
            var key = @event.Channel.Substring(0, index);
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
