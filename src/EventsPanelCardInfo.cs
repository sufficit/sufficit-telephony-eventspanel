using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    /// <summary>
    /// Basic info to Match a card to a monitor
    /// </summary>
    public class EventsPanelCardInfo : IEventsPanelCardInfo, IEquatable<EventsPanelCardInfo>
    {
        public EventsPanelCardInfo()
        {
            Exclusive = true;
            Label = "Unknown";
            Channels = new HashSet<string>();
        }

        public HashSet<string> Channels { get; set; }

        public virtual EventsPanelCardKind Kind { get; set; }

        public string Label { get; set; }

        public bool Exclusive { get; set; }

        public bool Equals(EventsPanelCardInfo? other)
            => other != null &&
            other.Kind == Kind &&
            other.Label == Label &&
            other.Exclusive == Exclusive &&
            other.Channels.SetEquals(Channels);
    }
}
