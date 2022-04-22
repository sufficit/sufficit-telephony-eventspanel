using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class EventsPanelCardCollection : ObservableCollection<EventsPanelCardMonitor>, IEventsPanelCardCollection
    {
        public IEnumerable<EventsPanelCardMonitor> this[string key]
            => (this.Where(s => s.IsChannelMatch(key)) ?? Array.Empty<EventsPanelCardMonitor>())
            .OrderBy(o => !o.Exclusive)
            ;


        public virtual void AddCard(EventsPanelCardMonitor card)
        {
            Add(card); 
        }
    }

    public class EventsPanelCardGroupedCollection : ObservableCollection<EventsPanelCardMonitor>, IEventsPanelCardCollection
    {
        public IEnumerable<EventsPanelCardMonitor> this[string key]
            => (this.Where(s => s.IsChannelMatch(key)) ?? Array.Empty<EventsPanelCardMonitor>())
            .OrderBy(o => !o.Exclusive)
            ;

        public void AddCard(EventsPanelCardMonitor card)
        {
            Add(card);
        }
    }
}
