using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Sufficit.Telephony.EventsPanel.IMonitor;

namespace Sufficit.Telephony.EventsPanel
{

    public interface IEventsPanelCardCollection : ICollection<EventsPanelCard>
    {
        IEnumerable<EventsPanelCard> this[string key] { get; }

        IList<EventsPanelCard> ToList();

        IList<T> ToList<T>();

        event Action<EventsPanelCard?>? OnChanged;

        IEnumerable<EventsPanelTrunkCard> Trunks { get; }

        IEnumerable<EventsPanelPeerCard> Peers { get; }

        IEnumerable<EventsPanelQueueCard> Queues { get; }
    }
}
