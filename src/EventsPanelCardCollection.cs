using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class EventsPanelCardCollection : CardCollection<EventsPanelCard>, IEventsPanelCardCollection
    {
        /// <summary>
        /// Sending lock object to base, multi thread support
        /// </summary>
        public EventsPanelCardCollection(): base(new object()) { }

        IEnumerable<EventsPanelCard> IEventsPanelCardCollection.this[string key]
        {
            get
            {
                var matches = this.Where(s => s.IsMatch(key)).ToList();
                if(matches != null)
                {
                    return matches.OrderBy(o => !o.Info.Exclusive); 
                }                    
                
                return Array.Empty<EventsPanelCard>();
            }
        }

        public IEnumerable<EventsPanelTrunkCard> Trunks
            => ToList<EventsPanelTrunkCard>();

        public IEnumerable<EventsPanelPeerCard> Peers
            => ToList<EventsPanelPeerCard>()
            .OrderBy(s => s.Info.Label);

        public IEnumerable<EventsPanelQueueCard> Queues
           => ToList<EventsPanelQueueCard>()
             .OrderBy(s => s.Info.Label);
    }
}
