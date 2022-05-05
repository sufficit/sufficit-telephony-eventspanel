using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public interface IEventsPanelHandler : IMatch, IMonitor
    {
        void Event(object @event);
    }
}
