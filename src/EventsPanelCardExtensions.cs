using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public static class EventsPanelCardExtensions
    {
        public static string? GetPeerKey(this EventsPanelCardInfo source)
        {
            /*
            if (!string.IsNullOrWhiteSpace(source.Key))
                return source.Key;
            
            else*/
            if (source.Channels.Count == 1 && source.Exclusive)
            {
                string s = source.Channels.First();
                s = s.TrimStart('^');
                s = s.TrimStart('*');
                return s;
            }
            return null;
        }


        public static string GetQueueKey(this EventsPanelCardInfo source)
        {
            if (source.Channels.Count == 1 && source.Exclusive)
            {
                string s = source.Channels.First();
                s = s.TrimStart('^');
                s = s.TrimStart('*');
                if (s.Contains('/'))
                    s = s.Split('/')[1];
                
                return s;
            }
            return source.Label;
        }

        public static EventsPanelCard CardCreate(this EventsPanelCardInfo source, EventsPanelService service)
        {
            switch (source.Kind)
            {
                case EventsPanelCardKind.QUEUE:
                    {
                        var key = source.GetQueueKey();
                        var monitor = EventsPanelService.Monitor(service.Queues, key);
                        return new EventsPanelQueueCard(source, monitor);
                    }
                case EventsPanelCardKind.PEER:
                    {
                        var key = source.GetPeerKey();
                        if (key != null)
                        {
                            var monitor = EventsPanelService.Monitor(service.Peers, key);
                            return new EventsPanelPeerCard(source, monitor);
                        } else
                            return new EventsPanelPeerCard(source);
                    }
                case EventsPanelCardKind.TRUNK:
                    {
                        var key = source.GetPeerKey();
                        if (key != null)
                        {
                            var monitor = EventsPanelService.Monitor(service.Peers, key);
                            return new EventsPanelTrunkCard(source, monitor);
                        }
                        else
                            return new EventsPanelTrunkCard(source);
                    }
                default: throw new NotImplementedException();
            }
        }


        public static EventsPanelCard CardMonitor(this EventsPanelCardInfo source, EventsPanelService service)
        {
            switch (source.Kind)
            {
                case EventsPanelCardKind.QUEUE:
                    {
                        var key = source.GetQueueKey();
                        var monitor = service.Queues[key];
                        if (monitor != null)
                            return new EventsPanelQueueCard(source, monitor);
                        else throw new InvalidDataException($"invalid key: {key}, for queue.");
                    }
                case EventsPanelCardKind.PEER:
                    {
                        var key = source.GetPeerKey();
                        if (key != null)
                        {
                            var monitor = service.Peers[key];
                            if (monitor != null)
                                return new EventsPanelPeerCard(source, monitor);
                            else throw new InvalidDataException($"invalid key: {key}, for peer.");
                        }
                        else
                            return new EventsPanelPeerCard(source);
                    }
                case EventsPanelCardKind.TRUNK:
                    {
                        var key = source.GetPeerKey();
                        if (key != null)
                        {
                            var monitor = service.Peers[key];
                            if (monitor != null)
                                return new EventsPanelTrunkCard(source, monitor);
                            else throw new InvalidDataException($"invalid key: {key}, for trunk peer.");
                        }
                        else
                            return new EventsPanelTrunkCard(source);
                    }
                default: throw new NotImplementedException();
            }
        }

        public static T? Content<T>(this EventsPanelCard source) => (T?)source.Monitor?.Content;
    }
}
