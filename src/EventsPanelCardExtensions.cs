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

        /// <summary>
        /// Sincroniza channels ativos do service para o card.
        /// Verifica todos os channels existentes e adiciona ao card se houver match.
        /// </summary>
        private static void SyncChannelsToCard(EventsPanelCard card, EventsMonitorService service)
        {
            foreach (var channel in service.Channels)
            {
                // Verifica se channel corresponde a este card
                if (card.IsMatch(channel.Content.Key))
                {
                    // Adiciona se ainda não existe
                    if (!card.Channels.Contains(channel))
                    {
                        card.Channels.Add(channel);
                    }
                }
            }
        }

        /// <summary>
        ///     Used for creating cards and monitor from sufficit endpoints request
        /// </summary>
        public static EventsPanelCard CardCreate(this EventsPanelCardInfo source, EventsMonitorService service)
        {
            EventsPanelCard card;
            
            switch (source.Kind)
            {
                case EventsPanelCardKind.QUEUE:
                    {
                        var key = source.GetQueueKey();
                        var monitor = service.Queues.Monitor(key);
                        card = new EventsPanelQueueCard(source, monitor);
                        break;
                    }
                case EventsPanelCardKind.PEER:
                    {
                        var key = source.GetPeerKey();
                        if (key != null)
                        {
                            var monitor = service.Peers.Monitor(key);
                            card = new EventsPanelPeerCard(source, monitor);
                        } 
                        else
                        {
                            card = new EventsPanelPeerCard(source);
                        }
                        break;
                    }
                case EventsPanelCardKind.TRUNK:
                    {
                        var key = source.GetPeerKey();
                        if (key != null)
                        {
                            var monitor = service.Peers.Monitor(key);
                            card = new EventsPanelTrunkCard(source, monitor);
                        }
                        else
                        {
                            card = new EventsPanelTrunkCard(source);
                        }
                        break;
                    }
                default: 
                    throw new NotImplementedException();
            }
        

            SyncChannelsToCard(card, service);
            
            return card;
        }


        public static EventsPanelCard CardMonitor(this EventsPanelCardInfo source, EventsPanelService service)
        {
            EventsPanelCard card;
             
             switch (source.Kind)
            {
                case EventsPanelCardKind.QUEUE:
                    {
                        var key = source.GetQueueKey();
                        var monitor = service.Queues[key];
                        if (monitor != null)
                        {
                            card = new EventsPanelQueueCard(source, monitor);
                        }
                        else 
                        {
                            throw new InvalidDataException($"invalid key: {key}, for queue.");
                        }
                        break;
                    }
                case EventsPanelCardKind.PEER:
                    {
                        var key = source.GetPeerKey();
                        if (key != null)
                        {
                            var monitor = service.Peers[key];
                            if (monitor != null)
                            {
                                card = new EventsPanelPeerCard(source, monitor);
                            }
                            else 
                            {
                                throw new InvalidDataException($"invalid key: {key}, for peer.");
                            }
                        }
                        else
                        {
                            card = new EventsPanelPeerCard(source);
                        }
                        break;
                    }
                case EventsPanelCardKind.TRUNK:
                    {
                        var key = source.GetPeerKey();
                        if (key != null)
                        {
                            var monitor = service.Peers[key];
                            if (monitor != null)
                            {
                                card = new EventsPanelTrunkCard(source, monitor);
                            }
                            else 
                            {
                                throw new InvalidDataException($"invalid key: {key}, for trunk peer.");
                            }
                        }
                        else
                        {
                            card = new EventsPanelTrunkCard(source);
                        }
                        break;
                    }
                default: 
                    throw new NotImplementedException();
            }
        
            SyncChannelsToCard(card, service);
            
            return card;
        }

        public static T? Content<T>(this EventsPanelCard source) => (T?)source.Monitor?.Content;
    }
}
