using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public static class EventsPanelServiceExtensions
    {
        public static bool CardCreate(this EventsPanelService source, EventsPanelCardInfo info, out EventsPanelCard? cardResult)
        {
            switch (info.Kind)
            {
                case EventsPanelCardKind.QUEUE:
                    {
                        var key = info.GetQueueKey();
                        var monitor = source.Queues.Monitor(key);
                        cardResult = new EventsPanelQueueCard(info, monitor);
                        break;
                    }
                case EventsPanelCardKind.PEER:
                    {
                        var key = info.GetPeerKey();
                        if (key != null)
                        {
                            var monitor = source.Peers.Monitor(key);
                            cardResult =new EventsPanelPeerCard(info, monitor);
                        }
                        else
                            cardResult =new EventsPanelPeerCard(info);
                        break;
                    }
                case EventsPanelCardKind.TRUNK:
                    {
                        var key = info.GetPeerKey();
                        if (key != null)
                        {
                            var monitor = source.Peers.Monitor(key);
                            cardResult =new EventsPanelTrunkCard(info, monitor);
                        }
                        else
                            cardResult =new EventsPanelTrunkCard(info);
                        break;
                    }
                default: cardResult = null; return false;
            }

            source.Append(cardResult);
            return true;
        }

        public static IEnumerable<EventsPanelCard> GetVisibles(this Panel? source)
        {
            if (source != null)
            {
                foreach (EventsPanelPeerCard item in source.Cards.OfType<EventsPanelPeerCard>())
                {
                    if (item.IsRendered)
                        yield return item;
                }
            }
        }
    }
}
