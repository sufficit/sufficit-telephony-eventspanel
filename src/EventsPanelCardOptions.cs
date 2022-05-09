using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class EventsPanelCardOptions : ICollection<EventsPanelCardInfo>
    {
        public const string SECTIONNAME = "Sufficit:Telephony:EventsPanel:Cards";

        private readonly List<EventsPanelCardInfo> _cards;

        public EventsPanelCardOptions()
        {
            _cards = new List<EventsPanelCardInfo>();
        }

        public int Count => _cards.Count;

        public bool IsReadOnly => false;

        public void Clear() => _cards.Clear();

        public bool Contains(EventsPanelCardInfo item) => _cards.Contains(item);

        public void CopyTo(EventsPanelCardInfo[] array, int arrayIndex) => _cards.CopyTo(array, arrayIndex);

        public bool Remove(EventsPanelCardInfo item) => _cards.Remove(item);

        public IEnumerator<EventsPanelCardInfo> GetEnumerator() => _cards.GetEnumerator();

        public void Add(EventsPanelCardInfo item) => _cards.Add(item);

        IEnumerator IEnumerable.GetEnumerator() => _cards.GetEnumerator();
    }
}
