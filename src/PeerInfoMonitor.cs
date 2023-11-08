using Sufficit.Asterisk.Manager.Events;
using Sufficit.Asterisk.Manager.Events.Abstracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class PeerInfoMonitor : EventsPanelMonitor<PeerInfo>
    {
        public PeerInfoMonitor(string key) : base(new PeerInfo(key)) { }

        #region IMPLEMENT ABSTRACT MONITOR CONTENT

        public override void Event(object @event)
        {
            DateTime evtts = DateTime.UtcNow;
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
