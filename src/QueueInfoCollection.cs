using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class QueueInfoCollection : MonitorCollection<QueueInfoMonitor>
    {
        /// <summary>
        /// GetOrCreate Monitor
        /// </summary>
        public QueueInfoMonitor Monitor(string key)
        {
            var monitor = this[key];
            if (monitor == null)
            {
                monitor = new QueueInfoMonitor(key);
                Add(monitor);
            }
            return monitor;
        }
    }
}
