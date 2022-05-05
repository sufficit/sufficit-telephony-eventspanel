using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class EventsPanelCardCollection : GenericCollection<EventsPanelCardMonitor>, IEventsPanelCardCollection
    {
        public new IEnumerable<EventsPanelCardMonitor> this[string key]
        {
            get
            {
                var matches = this.Where(s => s.IsMatch(key));
                if(matches != null)
                {
                    return matches.OrderBy(o => !o.Card.Exclusive).ToList(); 
                }                    
                
                return Array.Empty<EventsPanelCardMonitor>();
            }
        }
    }
}
