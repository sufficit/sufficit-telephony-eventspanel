using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class EventsPanelTrunkCard : EventsPanelCard<PeerInfo>
    {
        public new PeerInfoMonitor? Monitor => (PeerInfoMonitor?)base.Monitor;

        public EventsPanelTrunkCard(EventsPanelCardInfo card) : base(card)
        {

        }

        public EventsPanelTrunkCard(EventsPanelCardInfo card, PeerInfoMonitor monitor) : base(card, monitor)
        {

        }
    }
}
