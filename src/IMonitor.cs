using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public interface IMonitor : IKey
    {       
        /// <summary>
        /// On item changed
        /// </summary>
        event Action<IMonitor, object?>? OnChanged;
    }
}
