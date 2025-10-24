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
        /// Indicates that this panel has any card configured
        /// </summary>
        /// <returns></returns>
        public bool HasCards()
            => Cards.Any();

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
            _service.OnCardsChanged += OnCardsChanged;
        }

        public Panel(EventsPanelOptions options, EventsPanelService service)
        {
            _service = service;
            Cards = new EventsPanelCardCollection();
            Options = options;

            _service.OnEvent += OnEvent;
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

        private void OnEvent(IEnumerable<string> cardKeys, IManagerEventFromAsterisk @event)
        {
            foreach (var key in cardKeys)
            {
                var cards = Cards[key];
                foreach (var card in cards)
                {
                    if (card.Monitor != null)
                        card.Monitor.Event(@event);

                    if (@event is IChannelEvent channelEvent)
                    {
                        var channelKey = _service.Channels.HandleEvent(channelEvent);
      
                        var channelMonitor = _service.Channels[channelEvent.Channel];
              
                        if (channelMonitor != null && card.IsMatch(channelKey))
                        {
                            if (!card.Channels.Contains(channelMonitor))
                                card.Channels.Add(channelMonitor);
                        }
                    }

                    if (card.Info.Exclusive) break;
                }
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
