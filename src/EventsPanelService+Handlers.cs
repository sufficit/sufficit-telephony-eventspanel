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
        public static ChannelInfoMonitor HandleEvent(EventsPanelService source, IChannelEvent @event)
        {
            var key = @event.Channel;
            var monitor = source.Channels[key];
            if (monitor == null)
            {
                monitor = new ChannelInfoMonitor(key);
                source.Channels.Add(monitor);
            }

            monitor.Event(@event);
            return monitor;
        }

        public static PeerInfoMonitor HandleEvent(EventsPanelService source, IPeerStatus @event)
        {
            var key = @event.Peer;
            var monitor = source.Peers[key];
            if (monitor == null)
            {
                monitor = new PeerInfoMonitor(key);
                source.Peers.Add(monitor);
            }

            monitor.Event(@event);           
            return monitor;
        }

        public static QueueInfoMonitor HandleEvent(EventsPanelService source, IQueueEvent @event)
        {
            var key = @event.Queue;
            var monitor = source.Queues[key];
            if (monitor == null)
            {
                monitor = new QueueInfoMonitor(key);
                source.Queues.Add(monitor);
            }

            monitor.Event(@event);
            return monitor;
        }
    }
}
