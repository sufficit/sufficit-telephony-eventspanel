using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class PeerInfoCollection : MonitorCollection<PeerInfoMonitor>
    {
        /// <summary>
        ///     GetOrCreate Monitor
        /// </summary>
        public PeerInfoMonitor Monitor(string key, bool permanent = false)
        {
            var monitor = this[key];
            if (monitor == null)
            {
                monitor = new PeerInfoMonitor(key);
                monitor.Permanent = permanent;
                Add(monitor);
            }
            return monitor;
        }
    }
}
