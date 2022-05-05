using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class EventsPanelServiceOptions : IEquatable<EventsPanelServiceOptions>
    {
        public const string SECTIONNAME = "Sufficit:Telephony:EventsPanel";

        public EventsPanelServiceOptions()
        {
            IgnoreLocal = true;
            Cards = new List<EventsPanelCard>();
        }

        public int MaxButtons { get; set; }

        public bool AutoFill { get; set; }

        public bool IgnoreLocal { get; set; }

        public ICollection<EventsPanelCard> Cards { get; }

        public bool Equals(EventsPanelServiceOptions? other)
            => other != null && 
            other.MaxButtons == MaxButtons &&
            other.AutoFill == AutoFill &&
            other.IgnoreLocal == IgnoreLocal;        
    }
}
