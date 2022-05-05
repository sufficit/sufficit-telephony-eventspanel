using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public interface IMonitor : IKey
    {       
        event AsyncEventHandler? OnChanged;

        delegate void AsyncEventHandler(IMonitor? sender, object? state);
    }
}
