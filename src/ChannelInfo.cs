using Sufficit.Asterisk;
using Sufficit.Asterisk.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class ChannelInfo
    {
        public ChannelInfo(string id) => Id = id;

        public string Id { get; set; }

        public AsteriskChannelState State { get; set; }

        public Hangup? Hangup { get; set; }
    }
}
