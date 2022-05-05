using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class EventsPanelQueueCard : EventsPanelCard
    {
        public override EventsPanelCardKind Kind => EventsPanelCardKind.QUEUE;

        public EventsPanelQueueCard() { }

        public EventsPanelQueueCard(EventsPanelCard source)
        {
            this.Label = source.Label;
            this.Exclusive = source.Exclusive;
            
            foreach(var s in source.Channels)
                this.Channels.Add(s);
        }
    }
}
