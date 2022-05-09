using Sufficit.Asterisk.Manager.Events;
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
            if (@event is IPeerStatus newEvent)
            {
                // Console.WriteLine($"IPeerStatus({ @event.GetType() }): { newEvent.Peer } :: { newEvent.PeerStatus }");
                var currentState = newEvent.PeerStatus;
                if (Content.Status != currentState)
                {
                    Content.Status = currentState;
                    base.Event(@event);
                }
            }
        }

        #endregion

        public static implicit operator PeerInfo (PeerInfoMonitor source)
            => source.Content;
    }
}
