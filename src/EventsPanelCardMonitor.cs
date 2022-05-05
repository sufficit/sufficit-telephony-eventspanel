using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public abstract class EventsPanelCardMonitor : EventsPanelMonitor
    {
        private readonly EventsPanelMemoryCache<IManagerEvent> _buffer;

        private readonly EventsPanelCard _card;

        public EventsPanelCardMonitor(EventsPanelCard card, IKey content) : base(content)
        {
            _buffer = new EventsPanelMemoryCache<IManagerEvent>();
            _card = card;

            Channels = new ChannelInfoCollection();
        }

        public string Label => _card.Label;

        public ChannelInfoCollection Channels { get; }

        /// <summary>
        /// Last event time of this monitor
        /// </summary>
        public DateTime MaxUpdate { get; set; }

        /// <summary>
        /// First event time of this monitor
        /// </summary>
        public DateTime MinUpdate { get; set; }

        public override bool IsMatch(string key)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                #region CHECKING PEER IF EXISTS

                if (base.IsMatch(key))
                    return true;

                #endregion
                #region CHECKING CHANNELS

                var keyNormalized = key.Trim().ToLowerInvariant();
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

        /// <summary>
        /// Get the underlaying card from that monitor <br />
        /// Can be overrited
        /// </summary>
        public virtual EventsPanelCard Card => _card;

        /// <summary>
        /// Get the underlaying card from that monitor
        /// </summary>
        /// <param name="monitor"></param>

        public static implicit operator EventsPanelCard(EventsPanelCardMonitor monitor)
        {
            return monitor._card;
        }
    }

    public class EventsPanelCardMonitor<T> : EventsPanelCardMonitor where T : IKey
    {
        public new T Content => (T)base.Content;

        public EventsPanelCardMonitor(EventsPanelCard card)
            : base(card, new EmptyMonitor()) { }

        public EventsPanelCardMonitor(EventsPanelCard card, T content)
            : base(card, content!) { }

        public static implicit operator T (EventsPanelCardMonitor<T> source)
            => source.Content;
    }
}
