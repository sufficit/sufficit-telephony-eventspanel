using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public static class EventsPanelCardExtensions
    {
        /*
        public static IEventsPanelHandler Handler(this EventsPanelCard source)
        {            
            switch (source.Kind)
            {
                case EventsPanelCardKind.QUEUE:     return new QueueInfoMonitor(source);
                case EventsPanelCardKind.PEER:      return new PeerInfoMonitor(source.Key);
                case EventsPanelCardKind.TRUNK:     return new PeerInfoMonitor(source.Key);
                default: throw new NotImplementedException();
            }            
        }
        */

        public static bool IsValidPeer(string s)
        {
            if (!string.IsNullOrWhiteSpace(s))
            {

            }
            return false;
        }

        public static string? GetPeer(this EventsPanelCard source)
        {
            if (!string.IsNullOrWhiteSpace(source.Key))
                return source.Key;
            else if (source.Channels.Count == 1)
            {
                string s = source.Channels.First();
                s = s.TrimStart('^');
                s = s.TrimStart('*');
                return s;
            }
            return null;
        }

        public static EventsPanelCardMonitor CardMonitor(this EventsPanelCard source, EventsPanelService service)
        {
            switch (source.Kind)
            {
                case EventsPanelCardKind.QUEUE: return new EventsPanelCardMonitor<QueueInfoMonitor>(source, new QueueInfoMonitor(source.Label));
                case EventsPanelCardKind.PEER:
                case EventsPanelCardKind.TRUNK:
                    {
                        string? key  = source.GetPeer();
                        if (key != null)
                        {
                            var monitor = service.Peers[key];
                            if (monitor == null)
                            {
                                monitor = new PeerInfoMonitor(key);
                                service.Peers.Add(monitor);
                            }
                            return new EventsPanelCardMonitor<PeerInfoMonitor>(source, monitor);
                        }
                        return new EventsPanelCardMonitor<PeerInfoMonitor>(source);
                    }
                default: throw new NotImplementedException();
            }
        }

        public static T Content<T>(this EventsPanelCardMonitor source) => (T)source.Content;
    }
}
