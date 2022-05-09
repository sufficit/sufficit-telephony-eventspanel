using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class EventsPanelServiceOptions : IEquatable<EventsPanelServiceOptions>, IEventsPanelOptions
    {
        public const string SECTIONNAME = "Sufficit:Telephony:EventsPanel";

        public EventsPanelServiceOptions()
        {
            IgnoreLocal = true;
            ShowTrunks = true;
            Cards = new List<EventsPanelCardInfo>();
        }

        /// <summary>
        /// Value in Milliseconds <br />  
        /// If RefreshRate == 0, FastReload, RealTime operation, may crash WASM 
        /// </summary>
        public uint RefreshRate { get; set; }

        public bool ShowTrunks { get; set; }

        public int MaxButtons { get; set; }

        public bool AutoFill { get; set; }

        public bool IgnoreLocal { get; set; }

        public ICollection<EventsPanelCardInfo> Cards { get; }

        public bool Equals(EventsPanelServiceOptions? other)
            => other != null && 
            other.RefreshRate == RefreshRate &&
            other.ShowTrunks == ShowTrunks &&
            other.MaxButtons == MaxButtons &&
            other.AutoFill == AutoFill &&
            other.IgnoreLocal == IgnoreLocal;        
    }
}
