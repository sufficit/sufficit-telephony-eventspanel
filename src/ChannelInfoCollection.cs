using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class ChannelInfoCollection : ObservableCollection<ChannelInfoMonitor>
    {
        private readonly object _lock;
        public ChannelInfoCollection()
        {
            _lock = new object();
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);

            if (e.NewItems != null) {
                foreach (var item in e.NewItems.Cast<ChannelInfoMonitor>())
                {
                    item.Changed += Item_Changed;
                } 
            }
        }

        private async void Item_Changed(object? sender, Asterisk.AsteriskChannelState e)
        {
            if(sender is ChannelInfoMonitor monitor)
            {
                if(monitor.Hangup != null)
                {
                    await Task.Delay(5000);
                    lock (_lock)
                    {
                        if (Contains(monitor)) { Remove(monitor); }
                    }
                }
            }
        }

        public ChannelInfoMonitor? this[string key] 
            => this.FirstOrDefault(s => s.Id == key);
    }
}
