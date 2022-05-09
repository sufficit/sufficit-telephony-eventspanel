using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class ChannelInfoCollection : GenericCollection<ChannelInfoMonitor>
    {
        public override void Add(ChannelInfoMonitor monitor)
        {
            base.Add(monitor);

            var content = monitor.GetContent();
            if (content.Hangup != null)                
                ItemChanged(monitor, null);   
        }

        public override async void ItemChanged(IMonitor? sender, object? state)
        {
            if (sender != null && sender is ChannelInfoMonitor monitor)
            {
                var content = monitor.GetContent();
                if (content.Hangup != null)
                {
                    await Task.Delay(5000);                    
                    Remove(monitor);                    
                }
            }
        }
    }
}
