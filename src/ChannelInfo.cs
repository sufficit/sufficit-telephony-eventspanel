using Sufficit.Asterisk;
using Sufficit.Asterisk.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class ChannelInfo : IKey
    {
        #region IMPLEMENT INTERFACE KEY

        public string Key { get; }

        #endregion

        public ChannelInfo(string key) => Key = key;

        public DateTime Start { get; set; }

        public AsteriskChannelState State { get; set; }

        public Hangup? Hangup { get; set; }

        public string? CallerIDNum { get; set; }

        public string? CallerIDName { get; set; }
    }
}
