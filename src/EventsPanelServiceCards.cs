using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class EventsPanelServiceCards : HashSet<EventsPanelCardInfo>, IEquatable<EventsPanelServiceCards>
    {
        public const string SECTIONNAME = EventsPanelServiceOptions.SECTIONNAME + ":Cards";

        public bool Equals(EventsPanelServiceCards? other)
            => other != null && 
            other.SetEquals(this);        
    }
}
