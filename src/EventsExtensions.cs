using Sufficit.Asterisk;
using Sufficit.Asterisk.Events;
using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public static class EventsExtensions
    {
        public static EventsPanelCardMonitor ToCard(this AMIPeerStatusEvent source)
        {
            var peerId = source.Peer;
            var card = new EventsPanelCard();
            card.Label = peerId;
            card.Peer = peerId;
            card.Channels.Add($"^{ peerId }");

            if (card.Label.Contains('/'))
            {
                var splitted = card.Label.Split('/');
                card.Label = splitted[1];
            }

            var monitor = new EventsPanelCardMonitor(card);
            monitor.State = source.PeerStatus;
            monitor.Address = source.Address;
            monitor.Cause = source.Cause;
            monitor.Time = source.Time;
            return monitor;
        }

        public static EventsPanelCardMonitor ToCard(this IChannelEvent source)
        {
            var channel = new AsteriskChannel(source.Channel);
            var peerId = channel.GetPeer();
            var card = new EventsPanelCard();
            card.Label = channel.Name;
            card.Peer = peerId;
            card.Channels.Add($"^{ peerId }");

            return new EventsPanelCardMonitor(card);
        }

        public static EventsPanelCardMonitor ToCard(this PeerEntryEvent source)
        {
            var card = new EventsPanelCard();

            var info = GetPeerInfo(source);
            card.Label = info.Name;
            card.Peer = info.GetDial();
            card.Channels.Add($"^{ card.Peer }");

            return new EventsPanelCardMonitor(card);
        }

        public static PeerInfo GetPeerInfo(this PeerEntryEvent source)
        {
            var info = new PeerInfo(source.ObjectName);
            info.Protocol = source.ChannelType;
            return info;
        }
    }
}
