using Sufficit.Asterisk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class PeerInfo
    {
        public PeerInfo(string name)
        {
            Name = name;
            Status = PeerStatus.Unknown;
        }

        public AsteriskChannelProtocol Protocol { get; set; }
        public string Name { get; set; }
        public PeerStatus Status { get; set; }

        public string GetDial() => $"{Protocol}/{Name}";
    }
}
