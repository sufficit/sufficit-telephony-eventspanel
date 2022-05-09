using Sufficit.Asterisk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class Peer
    {
        public Peer(AsteriskChannelProtocol prot, string name)
        {
            Protocol = prot;
            Name = name;
        }

        public Peer(string key)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                if (key.Contains('/'))
                {
                    var splitted = key.Split('/');
                    var tech = splitted[0];
                    Protocol = AsteriskChannelExtensions.ToAsteriskChannelProtocol(tech);
                    Name = splitted[1];
                }
                else Name = key; 
            }
            else { throw new ArgumentNullException("key"); }
        }

        public AsteriskChannelProtocol Protocol { get; }
        public string Name { get; }
    }
}
