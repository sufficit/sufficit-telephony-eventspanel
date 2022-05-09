using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class EventsPanelCardCollection : GenericCollection<EventsPanelCard>, IEventsPanelCardCollection
    {
        public new IEnumerable<EventsPanelCard> this[string key]
        {
            get
            {
                var matches = this.Where(s => s.IsMatch(key));
                if(matches != null)
                {
                    return matches.OrderBy(o => !o.Info.Exclusive).ToList(); 
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
