using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class EventsPanelTrunkCard : EventsPanelCard
    {
        public override EventsPanelCardKind Kind => EventsPanelCardKind.TRUNK;

        public EventsPanelTrunkCard() { }

        public EventsPanelTrunkCard(EventsPanelCard source)
        {
            this.Label = source.Label;
            this.Exclusive = source.Exclusive;

            foreach (var s in source.Channels)
                this.Channels.Add(s);
        }
    }
}
