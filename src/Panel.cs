using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class Panel
    {
        public Panel(IEventsPanelCardCollection cards)
        {
            Cards = cards;
            Options = new EventsPanelServiceOptions();
        }

        public virtual void Update(IEventsPanelOptions options) 
        {
            if (Options != options)
            {
                if (options != null)
                    Options = options;               
            }
        }

        public IEventsPanelOptions Options { get; internal set; }

        public IEventsPanelCardCollection Cards { get; }
    }
}
