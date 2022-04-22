using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class EventsPanelCardOptions : ICollection<EventsPanelCard>
    {
        public const string SECTIONNAME = "Sufficit:Telephony:EventsPanel:Cards";

        private readonly List<EventsPanelCard> _cards;

        public EventsPanelCardOptions()
        {
            _cards = new List<EventsPanelCard>();
        }

        public int Count => _cards.Count;

        public bool IsReadOnly => false;

        public void Clear() => _cards.Clear();

        public bool Contains(EventsPanelCard item) => _cards.Contains(item);

        public void CopyTo(EventsPanelCard[] array, int arrayIndex) => _cards.CopyTo(array, arrayIndex);

        public bool Remove(EventsPanelCard item) => _cards.Remove(item);

        public IEnumerator<EventsPanelCard> GetEnumerator() => _cards.GetEnumerator();

        public void Add(EventsPanelCard item) => _cards.Add(item);

        IEnumerator IEnumerable.GetEnumerator() => _cards.GetEnumerator();
    }
}
