using System;
using Sufficit.Asterisk;

namespace Sufficit.Telephony.EventsPanel
{
    public class PeerInfo : Peer, IKey
    {
        #region IMPLEMENT INTERFACE KEY

        string IKey.Key => $"{ Protocol }/{ Name }";

        #endregion

        public PeerInfo(string key) : base(key) { }
        public PeerStatus Status { get; set; }
        public PeerUnreachableCause? Cause { get; set; }
        public string? Address { get; set; }
        public int? Time { get; set; }
    }
}
