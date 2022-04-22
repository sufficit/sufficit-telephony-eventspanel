using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class Hangup
    {
        public int Code { get; set; }
        public string? Description { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
