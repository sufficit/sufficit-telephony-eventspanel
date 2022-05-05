using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Sufficit.Telephony.EventsPanel.IMonitor;

namespace Sufficit.Telephony.EventsPanel
{

    public interface IEventsPanelCardCollection : ICollection<EventsPanelCardMonitor>
    {
        IEnumerable<EventsPanelCardMonitor> this[string key] { get; }

        IList<EventsPanelCardMonitor> ToList();

        IList<T> ToList<T>();

        event AsyncEventHandler? OnChanged;
    }
}
