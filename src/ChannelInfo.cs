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

        public string? UniqueId { get; set; }
        public string? LinkedId { get; set; }

        /// <summary>
        /// First exten on new channel event
        /// </summary>
        public string? DialedExten { get; set; }
        public string? Exten { get; set; }
        public string? CallerIdNum { get; set; }
        public string? CallerIdName { get; set; }
        public string? ConnectedLineNum { get; set; }
        public string? ConnectedLineName { get; set; }


        public string? DirectInwardDialing { get; set; }

        public string? OutboundCallerId { get; set; }

        public CallDirection Direction { get; set; }

        /// <summary>
        /// Queue that originate that channel
        /// </summary>
        public string? Queue { get; set; }

        /// <summary>
        /// Queue abandoned
        /// </summary>
        public bool Abandoned { get; set; }

        /// <summary>
        /// Indicates if the channel is currently on hold (music on hold playing)
        /// </summary>
        public bool OnHold { get; set; }
    }
}
