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
        }

        public IEventsPanelCardCollection Cards { get; }
    }
}
