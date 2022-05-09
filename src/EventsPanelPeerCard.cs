using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class EventsPanelPeerCard : EventsPanelCard<PeerInfo>
    {
        public new PeerInfoMonitor? Monitor => (PeerInfoMonitor?)base.Monitor;

        public EventsPanelPeerCard(EventsPanelCardInfo card) : base(card)
        {

        }

        public EventsPanelPeerCard(EventsPanelCardInfo card, PeerInfoMonitor monitor) : base(card, monitor)
        {

        }
    }
}
