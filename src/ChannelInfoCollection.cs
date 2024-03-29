﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class ChannelInfoCollection : MonitorCollection<ChannelInfoMonitor>
    {
        public override void Add(ChannelInfoMonitor monitor)
        {
            base.Add(monitor);

            // checking if already hangup
            var content = monitor.GetContent();
            if (content.Hangup != null || content.Abandoned)                
                ItemChanged(monitor, null);            
        }

        protected override async void ItemChanged(IMonitor sender, object? state)
        {
            if (sender is ChannelInfoMonitor monitor)
            {
                var content = monitor.GetContent();
                if (content.Hangup != null || content.Abandoned)
                {
                    await Task.Delay(5000);                    
                    Remove(monitor);                    
                }
            }
        }

        /// <summary>
        /// GetOrCreate Monitor
        /// </summary>
        public ChannelInfoMonitor Monitor(string key, string? queue = null)
        {
            var monitor = this[key];
            if (monitor == null)
            {
                monitor = new ChannelInfoMonitor(key);
                monitor.Content.Queue = queue;
                Add(monitor);
            }
            return monitor;
        }
    }
}
