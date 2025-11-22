using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sufficit.Asterisk.Manager.Events;
using Sufficit.Identity;
using System.Collections.Specialized;

namespace Sufficit.Telephony.EventsPanel
{
    public partial class EventsPanelService : EventsMonitorService, IEventsPanelService
    {    
        #region IMPLEMENT INTERFACE EVENTSPANEL SERVICE

        public bool IsConfigured => EnsureValidHub();

        Task IEventsPanelService.ExecuteAsync(CancellationToken stoppingToken) => ExecuteAsync(stoppingToken);

        #endregion

        private readonly IDisposable? _optionsMonitor;
        private readonly ILogger _logger;
        private readonly IEventsPanelCardCollection _cards;
        private readonly IServiceProvider _provider;

        public ICollection<Exception> Exceptions { get; }

        public Uri? EndPoint => base.Options?.Endpoint;

        /// <summary>
        /// On base cards collection changed
        /// </summary>
        public event Action<EventsPanelCard?, NotifyCollectionChangedAction>? OnCardsChanged;
                
        public EventsPanelService(IServiceProvider provider) : base(
            provider.GetRequiredService<ILogger<EventsMonitorService>>(),
            provider.GetRequiredService< IOptionsMonitor<AMIHubClientOptions>>(),
            provider.GetRequiredService<ILogger<AMIHubClient>>()
            )
        {
            Exceptions = new List<Exception>();
            _provider = provider;

            var cardsImplementation = _provider.GetService<IEventsPanelCardCollection>();
            if (cardsImplementation != null)
                _cards = cardsImplementation;
            else 
                _cards = new EventsPanelCardCollection();

            // appending event handler
            _cards.OnChanged += OnCardsChanged;

            _logger = _provider.GetRequiredService<ILogger<EventsPanelService>>();
        
            // ITokenProvider is Scoped, so we need to create a scope to resolve it
            // This is safe because we only call it once during initialization
using (var scope = _provider.CreateScope())
            {
                var accesstokenprovider = scope.ServiceProvider.GetService<ITokenProvider>();
                if (accesstokenprovider != null)
                    AccessTokenProvider = accesstokenprovider.GetTokenAsync().AsTask();
                else
                    _logger.LogWarning("no token provider available");
            }

            var monitor = _provider.GetRequiredService<IOptionsMonitor<EventsPanelServiceOptions>>();
            _optionsMonitor = monitor.OnChange(Configure);      

            _logger.LogTrace($"Serviço de Controle { GetType().Name } construído !");
        }

        public void Configure (EventsPanelServiceOptions options)
        {
            if (!options.Equals(Options))
            {
                Options = options;
                _logger.LogInformation($"options updated, max buttons: {Options.MaxButtons}, cards: {Options.Cards.Count()}, show only peers: {Options.OnlyPeers}");

                if (Options.Cards.Any())
                {
                    foreach (var card in Options.Cards)
                    {
                        var cardMonitor = EventsPanelCardExtensions.CardCreate(card, this);
                        _cards.Add(cardMonitor);
                    }
                }          
            }
        }

        public void ChannelsCleanUp()
        {
            foreach(var item in Channels.ToList())
            {
                if (item.Timestamp.AddMinutes(20) < DateTime.UtcNow)
                {
                    Channels.Remove(item);
                }
            }
        }

        public new EventsPanelServiceOptions? Options { get; internal set; }
                        
        #region EVENT HANDLERS

        protected bool LimitReached => Options?.MaxButtons > 0 && _cards.Count >= Options.MaxButtons;

        protected bool ShouldFill => !LimitReached && (Options == null || Options.AutoFill);

        public override bool IgnoreLocal => Options == null || Options.IgnoreLocal;

        protected bool ShouldFillPeers => ShouldFill;

        protected bool ShouldFillQueues => Options == null || Options.AutoGenerateQueueCards;

        protected IEnumerable<EventsPanelCard> HandleCardByKey(string key)
        {
            foreach (var card in _cards[key])
            {
                yield return card;
                if (card.Info.Exclusive) yield break;
            }
        }

        protected IEnumerable<EventsPanelCard> HandleCard(string key, IManagerEvent eventObj)
        {
            var cards = HandleCardByKey(key);
            if (cards.Any()) return cards;
                      
            if (!ShouldFill) return Array.Empty<EventsPanelCard>();

            var cardMonitor = eventObj.ToCard(key, this);

            //ignoring peers auto fill
            if (!ShouldFillPeers && cardMonitor.Info.Kind == EventsPanelCardKind.PEER)
                return Array.Empty<EventsPanelCard>();

            if (!ShouldFillQueues && cardMonitor.Info.Kind == EventsPanelCardKind.QUEUE)
                return Array.Empty<EventsPanelCard>();

            _logger.LogDebug($"adding a new card, kind: {cardMonitor.Info.Kind}, search key: { key }, card keys: { string.Join('|', cardMonitor.Keys) }");
            _cards.Add(cardMonitor); // include global

            return new[] { cardMonitor };
        }

        #endregion        

        /// <summary>
        ///     Return base cards generated;
        /// </summary>
        public IEnumerable<EventsPanelCard> GetCards() => _cards;

        public bool Append (EventsPanelCard card)
        {
            if (!_cards.Contains(card))
            {
                _cards.Add(card);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Background task status
        /// </summary>
        public TaskStatus? Status => ExecuteTask?.Status ?? ExecuteTask?.Status;


        public delegate Task<string> AsyncTaskMonitor(EventsPanelCard monitor);
    }
}
