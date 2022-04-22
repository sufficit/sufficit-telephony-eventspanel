using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{

    public interface IEventsPanelCardCollection : INotifyCollectionChanged, ICollection<EventsPanelCardMonitor>
    {
        IEnumerable<EventsPanelCardMonitor> this[string key] { get; }

        void AddCard(EventsPanelCardMonitor card);
    }
}
