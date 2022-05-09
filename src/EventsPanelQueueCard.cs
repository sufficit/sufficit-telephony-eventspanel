using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class EventsPanelQueueCard : EventsPanelCard<QueueInfo>
    {
        public new QueueInfoMonitor? Monitor => (QueueInfoMonitor?)base.Monitor;

        public EventsPanelQueueCard(EventsPanelCardInfo card) : base(card)
        {

        }

        public EventsPanelQueueCard(EventsPanelCardInfo card, QueueInfoMonitor monitor) : base(card, monitor)
        {

        }
    }
}
