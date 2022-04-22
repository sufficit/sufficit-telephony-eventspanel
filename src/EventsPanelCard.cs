using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class EventsPanelCard : IEventsPanelCard
    {
        public EventsPanelCard()
        {
            Exclusive = true;
            Label = "Unknown";
            Channels = new HashSet<string>();
        }

        public EventsPanelCardKind Kind { get; set; }

        public HashSet<string> Channels { get; }

        public string Label { get; set; }

        public string? Peer { get; set; }

        public bool Exclusive { get; set; }
    }
}
