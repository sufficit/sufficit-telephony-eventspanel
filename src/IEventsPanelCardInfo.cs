using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public interface IEventsPanelCardInfo
    {
        /// <summary>
        /// This card has exclusivity to its channels <br />
        /// Do not continue after that
        /// </summary>
        bool Exclusive { get; }

        EventsPanelCardKind Kind { get; }

        string Label { get; }
    }
}
