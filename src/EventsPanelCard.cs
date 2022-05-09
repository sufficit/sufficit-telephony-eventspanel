using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public abstract class EventsPanelCard : IMonitor
    {
        public Guid UniqueId { get; }

        /// <summary>
        /// Indicates that the underlaynig monitor is not null
        /// </summary>
        public bool IsMonitored => Monitor != null;

        /// <summary>
        /// Card key not used for now, should ignore
        /// </summary>
        public string Key => UniqueId.ToString("N");

        public event IMonitor.AsyncEventHandler? OnChanged;

        public virtual EventsPanelMonitor? Monitor { get; internal set; }

        //private readonly EventsPanelMemoryCache<IManagerEvent> _buffer;

        private readonly EventsPanelCardInfo _info;


        public EventsPanelCard(EventsPanelCardInfo info)
        {
            UniqueId = Guid.NewGuid();

            //_buffer = new EventsPanelMemoryCache<IManagerEvent>();
            _info = info;

            Channels = new ChannelInfoCollection();
        }

        public EventsPanelCard(EventsPanelCardInfo info , EventsPanelMonitor monitor) : this(info)
        {
            Monitor = monitor;
        }

        public string Label => _info.Label;

        public ChannelInfoCollection Channels { get; }

        /// <summary>
        /// Last event time of this monitor
        /// </summary>
        public DateTime MaxUpdate { get; set; }

        /// <summary>
        /// First event time of this monitor
        /// </summary>
        public DateTime MinUpdate { get; set; }
                
        /// <summary>
        /// Used to filter channels that should be showing in this card
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool IsMatch(string key)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                // checking peer or queue
                if (IsMonitored && Monitor!.IsMatch(key))
                    return true;

                #region CHECKING CHANNELS

                var keyNormalized = key.Trim().ToLowerInvariant();
                foreach (var item in _info.Channels)
                {
                    var match = new EventsPanelChannelMatch(item);
                    if (match.IsMatch(keyNormalized))
                        return true;
                }

                #endregion
            }

            return false;
        }        

        public bool IsMatchFilter(string key)
        {
            var keyNormalized = key.Trim().ToLowerInvariant();
            if (Label.ToLowerInvariant().Contains(keyNormalized))
                return true;

            foreach (var item in _info.Channels)
            {
                if (item.ToLowerInvariant().Contains(keyNormalized))
                    return true;
            }
            return false;
        }


        /// <summary>
        /// Get the underlaying card from that monitor <br />
        /// Can be overrited
        /// </summary>
        public virtual EventsPanelCardInfo Info => _info;

        /// <summary>
        /// Get the underlaying card from that monitor
        /// </summary>
        /// <param name="monitor"></param>

        public static implicit operator EventsPanelCardInfo(EventsPanelCard monitor)
        {
            return monitor._info;
        }
    }

    public class EventsPanelCard<T> : EventsPanelCard where T : IKey
    {
        public new EventsPanelMonitor<T>? Monitor => (EventsPanelMonitor<T>?)base.Monitor;

        public T? Content => (T?)base.Monitor?.Content;

        public EventsPanelCard(EventsPanelCardInfo card)
            : base(card) { }

        public EventsPanelCard(EventsPanelCardInfo card, EventsPanelMonitor monitor)
            : base(card, monitor) { }

        public static implicit operator T? (EventsPanelCard<T> source)
            => source.Content;
    }
}
