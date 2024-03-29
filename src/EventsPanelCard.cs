﻿using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public abstract class EventsPanelCard : IMultipleKey, IEquatable<EventsPanelCardInfo>
    {
        public string[] Keys { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Get the underlaying card from that monitor <br />
        /// Can be overrited
        /// </summary>
        public virtual EventsPanelCardInfo Info => _info;

        /// <summary>
        /// Indicates that the underlaynig monitor is not null
        /// </summary>
        public bool IsMonitored => Monitor != null;

        /// <summary>
        /// Indicates that this object is active in use by frontend
        /// </summary>
        public bool IsRendered { get; set; }

        public string Label => _info.Label;

        public virtual EventsPanelMonitor? Monitor { get; internal set; }

        public ChannelInfoCollection Channels { get; }


        private readonly EventsPanelCardInfo _info;

        public EventsPanelCard(EventsPanelCardInfo info)
        {
            _info = info;
            Channels = new ChannelInfoCollection();
            Keys = new []{ Guid.NewGuid().ToString() }; 
        }

        public EventsPanelCard (EventsPanelCardInfo info , EventsPanelMonitor monitor) : this(info)
        {
            Monitor = monitor;
        }
                
        /// <summary>
        /// Used to filter channels that should be showing in this card
        /// </summary>
        public bool IsMatch(string key)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                var keyNormalized = key.Trim().ToLowerInvariant();
                if (Keys.Contains(keyNormalized))
                    return true;

                // checking peer or queue
                if (Monitor != null && Monitor.IsMatch(key))
                    return true;

                #region CHECKING CHANNELS

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

        public bool Equals(EventsPanelCardInfo? other)
        {
            if(other != null)
            {
                if(other.Equals(this.Info)) return true;
            }
            return false;
        }

        public override bool Equals(object? other)
            => other != null && other is EventsPanelCard p && p.Equals(this.Info);

        public override int GetHashCode()
            => this.Info.GetHashCode();

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
