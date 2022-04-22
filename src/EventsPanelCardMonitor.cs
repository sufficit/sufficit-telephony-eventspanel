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
    public class EventsPanelCardMonitor : IChannelMatch, IEventsPanelCard
    {
        #region IMPLEMENT INTERFACE EVENTSPANEL CARD

        public bool Exclusive => _card.Exclusive;

        public EventsPanelCardKind Kind => _card.Kind;

        public string Label => _card.Label;

        public string? Peer => _card.Peer;

        #endregion

        private readonly EventsPanelCard _card;
        public EventsPanelCardMonitor(EventsPanelCard card)
        {
            _card = card;
            Channels = new ChannelInfoCollection();
            Channels.CollectionChanged += Channels_CollectionChanged;
        }

        public AsteriskChannelProtocol Protocol { get; set; }
        public PeerStatus State { get; set; }
        public string? Address { get; set; }

        public ChannelInfoCollection Channels { get; }

        private void Channels_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //if(Channels.Count == 0)            
            Changed?.Invoke(this, State);
        }

        public PeerStatusCauseEnum Cause { get; set; }

        public int? Time { get; set; }

        public DateTime Update { get; set; }


        public event EventHandler<PeerStatus>? Changed;

        public async void Event(AMIPeerStatusEvent @event)
        {
            if (@event.DateReceived > Update)
            {
                Update = @event.DateReceived;
                if (State != @event.PeerStatus
                    || Address != @event.Address
                    || Cause != @event.Cause
                    || Time != @event.Time
                    )
                {
                    State = @event.PeerStatus;
                    Address = @event.Address;
                    Cause = @event.Cause;
                    Time = @event.Time;
                    Changed?.Invoke(this, State);
                }
            }

            await Task.CompletedTask;
        }

        public async void Event(PeerEntryEvent @event)
        {
            if (@event.DateReceived > Update)
            {
                Update = @event.DateReceived;
                var currentState = @event.GetPeerStatus();

                if (State != currentState)
                {
                    State = currentState;
                    Changed?.Invoke(this, State);
                }
            }

            await Task.CompletedTask;
        }

        public async void Event(IChannelEvent @event)
        {
            var channelId = @event.Channel;
            var channel = Channels[channelId];
            if (channel == null)
            {
                channel = new ChannelInfoMonitor(channelId);
                Channels.Add(channel);
            }

            await channel.Event(@event);
        }

        public bool IsChannelMatch(string key)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                var keyNormalized = key.Trim().ToLowerInvariant();

                #region CHECKING PEER IF EXISTS

                if (_card.Peer != null)
                {
                    var peerNormalized = _card.Peer.Trim().ToLowerInvariant();
                    if (!string.IsNullOrWhiteSpace(peerNormalized))
                    {
                        if (peerNormalized.Equals(keyNormalized))
                            return true;
                    }
                }

                #endregion
                #region CHECKING CHANNELS

                foreach (var item in _card.Channels)
                {
                    var match = new EventsPanelChannelMatch(item);
                    if (match.IsMatch(keyNormalized))
                        return true;
                }

                #endregion
            }

            return false;
        }
    }
}
