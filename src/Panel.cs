using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class Panel : IDisposable
    {
        /// <summary>
        /// Monitor changes on panel options
        /// </summary>
        public event Action<Panel>? OnChanged;

        private readonly EventsPanelService _service;

        public Panel(IEventsPanelCardCollection cards, EventsPanelService service)
        {
            _service = service;
            Cards = cards;
            Options = new EventsPanelOptions();

            _service.OnEvent += OnEvent;
        }

        /// <summary>
        /// Important for avoid duplicate event processing
        /// </summary>
        public void Dispose()
        {
            if(_service != null)
                _service.OnEvent -= OnEvent;
        }

        private void OnEvent(IEnumerable<string> keys, IManagerEventFromAsterisk @event)
        {
            foreach(string key in keys)
            {
                foreach (var card in Cards[key])
                {
                    Console.WriteLine($"card: { string.Join("|", card.Keys) }, label: { card.Label }, capturing event: { @event.GetType() }");
                    if (@event is IChannelEvent eventChannel)
                        card.IChannelEvent(_service.Channels, eventChannel);

                    if (@event is IPeerStatusEvent eventPeerStatus)
                        card.IPeerStatusEvent(eventPeerStatus);

                    if (@event is IQueueEvent eventQueue)
                        card.IQueueEvent(eventQueue);

                    switch (@event)
                    {
                        case PeerEntryEvent newEvent: card.Monitor?.Event(newEvent); break;
                        default: break;
                    }
                }      
            }
        }

        public virtual void Update(IEventsPanelOptions options) 
        {
            if (options != null && !options.Equals(Options))
            {
                Options = options;
                OnChanged?.Invoke(this);
            }
        }

        public virtual void Update(IEnumerable<EventsPanelCardInfo> cards, bool clear = false)
        {
            if(clear) Cards.Clear();
            foreach (var card in cards)
            {
                var cardMonitor = EventsPanelCardExtensions.CardCreate(card, _service);
                Cards.Add(cardMonitor);
            }

            OnChanged?.Invoke(this);
        }

        public IEventsPanelOptions Options { get; internal set; }

        public IEventsPanelCardCollection Cards { get; }
    }
}
