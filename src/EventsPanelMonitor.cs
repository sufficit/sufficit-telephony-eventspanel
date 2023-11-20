using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public abstract class EventsPanelMonitor : IEventsPanelMonitor, IEventsPanelHandler
    {
        public virtual IKey Content { get; }

        /// <summary>
        /// Index used on collections
        /// </summary>
        public string Key => Content.Key;

        /// <summary>
        /// Monitor changes on underlaying item properties, queue, peer, trunk <br />
        /// object => event that causes the change
        /// </summary>
        public event Action<IMonitor, object?>? OnChanged;

        /// <summary>
        /// Last event received at
        /// </summary>
        public DateTime Timestamp { get; protected set; }

        public EventsPanelMonitor(IKey content)
        {
            Content = content;
        }

        /// <summary>
        ///     State has changed
        /// </summary>
        public virtual void Event(object @event)
            => OnChanged?.Invoke(this, @event);        

        /// <summary>
        ///     Should match to peer or queue to show in card
        /// </summary>
        public virtual bool IsMatch(string match)
        {
            var peerNormalized = Key.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(peerNormalized))
            {
                if (peerNormalized.Equals(match, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }
    }

    public abstract class EventsPanelMonitor<T> : EventsPanelMonitor where T : IKey
    {
        public new T Content => (T)base.Content;

        public T GetContent() => (T)base.Content;

        public EventsPanelMonitor(T content) : base(content!) { }

        public static implicit operator T(EventsPanelMonitor<T> source) 
            => source.Content;
    }
}
