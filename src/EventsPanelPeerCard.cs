using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class EventsPanelPeerCard : EventsPanelCard
    {
        public override EventsPanelCardKind Kind => EventsPanelCardKind.PEER;

        public EventsPanelPeerCard() { }

        public EventsPanelPeerCard(EventsPanelCard source)
        {
            this.Label = source.Label;
            this.Exclusive = source.Exclusive;

            foreach (var s in source.Channels)
                this.Channels.Add(s);
        }
    }
}
