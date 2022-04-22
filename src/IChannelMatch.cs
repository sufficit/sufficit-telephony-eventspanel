using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public interface IChannelMatch
    {
        /// <summary>
        /// Indicates that the channel passed is match with these object <br />
        /// Common used by card to match channels to monitor
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool IsChannelMatch(string key);
    }
}
