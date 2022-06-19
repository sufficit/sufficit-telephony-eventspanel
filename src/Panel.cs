using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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

        public IEventsPanelOptions Options { get; internal set; }

        public IEventsPanelCardCollection Cards { get; }

        private readonly EventsPanelService _service;

        public Panel(IEventsPanelCardCollection cards, EventsPanelService service)
        {
            _service = service;
            Cards = cards;
            Options = new EventsPanelOptions();

            _service.OnEvent += OnEvent;

            if (Options.AutoFill)
                foreach (var card in _service.GetCards())
                    Cards.Add(card);

            _service.OnCardsChanged += OnCardsChanged;
        }

        private void OnCardsChanged(EventsPanelCard? obj, NotifyCollectionChangedAction action)
        {
            if (obj != null && Options.AutoFill)
            {
                Cards.Add(obj); 
                OnChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Important for avoid duplicate event processing
        /// </summary>
        public void Dispose()
        {
            if (_service != null)
            {
                _service.OnEvent -= OnEvent;
                _service.OnCardsChanged -= OnCardsChanged;
            }
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
                if (Options.AutoFill)
                {
                    foreach (var card in _service.GetCards())
                        if(!Cards.Contains(card))
                            Cards.Add(card);
                }

                OnChanged?.Invoke(this);
            }
        }

        public virtual void Update(IEnumerable<EventsPanelCardInfo> cards, bool clear = false)
        {
            if(clear) Cards.Clear();

            if (Options.AutoFill)
            {
                foreach (var card in _service.GetCards())
                    if (!Cards.Contains(card))
                        Cards.Add(card);
            }

            foreach (var card in cards)
            {
                var cardMonitor = EventsPanelCardExtensions.CardCreate(card, _service);
                if (!Cards.Contains(cardMonitor))
                    Cards.Add(cardMonitor);
            }            

            OnChanged?.Invoke(this);
        }

    }
}
