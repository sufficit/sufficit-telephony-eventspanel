using Sufficit.Asterisk.Manager.Events;
using System;

namespace Sufficit.Telephony.EventsPanel
{
    public class PeerInfoMonitor : EventsPanelMonitor<PeerInfo>
    {
        /// <summary>
        ///     Is a permanent monitor ? <br />
        ///     Should cleanup after long time inactive ?
        /// </summary>
        public bool Permanent { get; set; }

        public PeerInfoMonitor(string key) : base(new PeerInfo(key)) { }

        #region IMPLEMENT ABSTRACT MONITOR CONTENT

        public override void Event(object @event)
        {
            var evtts = DateTime.UtcNow;
            if (@event is IManagerEvent managerEvent)
                evtts = managerEvent.GetTimeStamp();

            if (evtts > Timestamp)
            {
                Timestamp = evtts;
                if (@event is IPeerStatus statusEvent)
                {
                    Content.Status = statusEvent.PeerStatus;

                    if (statusEvent is PeerStatusEvent peerStatusEvent)
                    {
                        Content.Time = peerStatusEvent.Time;
                        Content.Address = peerStatusEvent.Address;
                        Content.Cause = peerStatusEvent.Cause;
                    }
                }

                base.Event(@event);
            }
        }

        #endregion

        public static implicit operator PeerInfo (PeerInfoMonitor source)
            => source.Content;
    }
}
