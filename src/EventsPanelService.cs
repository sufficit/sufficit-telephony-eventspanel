using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sufficit.Asterisk;
using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public partial class EventsPanelService : IEventsPanelService
    {
        #region IMPLEMENT INTERFACE EVENTSPANEL SERVICE

        public bool IsConfigured => Client != null;

        public void Configure(AMIHubClientOptions options)
            => Configure(new AMIHubClient(options));

        Task IEventsPanelService.StartAsync(System.Threading.CancellationToken cancellationToken)
            => Client!.StartAsync(cancellationToken);

        Task IEventsPanelService.StopAsync(System.Threading.CancellationToken cancellationToken)
            => Client!.StopAsync(cancellationToken);

        #endregion

        private readonly ILogger _logger;
        private readonly IEventsPanelCardCollection _cards;
        private readonly IServiceProvider _provider;

        public ChannelInfoCollection Channels { get; }
        public PeerInfoCollection Peers { get; }
        public QueueInfoCollection Queues { get; }
        public ICollection<Exception> Exceptions { get; }


        public AMIHubClient? Client { get; internal set; }

        /// <summary>
        /// On Status Changed or Exception occurs
        /// </summary>
        public event Action<HubConnectionState?, Exception?>? OnChanged;

        /// <summary>
        /// On base cards collection changed
        /// </summary>
        public event Action<EventsPanelCard?, NotifyCollectionChangedAction>? OnCardsChanged;

        /// <summary>
        /// On event received from servers
        /// </summary>
        public event Action<IEnumerable<string>, IManagerEventFromAsterisk>? OnEvent;

        public EventsPanelService(IServiceProvider provider)
        {
            Exceptions = new List<Exception>();
            Channels = new ChannelInfoCollection();
            Peers = new PeerInfoCollection();
            Queues = new QueueInfoCollection();

            _provider = provider;

            var cardsImplementation = _provider.GetService<IEventsPanelCardCollection>();
            if (cardsImplementation != null)
                _cards = cardsImplementation;
            else 
                _cards = new EventsPanelCardCollection();

            // appending event handler
            _cards.OnChanged += OnCardsChanged;

            _logger = _provider.GetRequiredService<ILogger<EventsPanelService>>();

            var hubclientOptions = _provider.GetService<IOptions<AMIHubClientOptions>>();
            if (hubclientOptions != null && hubclientOptions.Value.Validate == null)
            {
                var client = _provider.GetService<AMIHubClient>();
                if (client != null) Configure(client);                
            }            

            Panel = new Panel(_cards, this);
            var monitor = _provider.GetService<IOptionsMonitor<EventsPanelServiceOptions>>();
            if (monitor != null)
            {
                OnConfigure(monitor.CurrentValue);
                monitor.OnChange(OnConfigure);
            }

            _logger.LogTrace($"Serviço de Controle { GetType().Name } construído !");
        }
               

        public async void Configure(AMIHubClient client)
        {
            if(client != null && !client.Equals(Client))
            {
                if(Client != null)                
                    await Client.DisposeAsync();

                Client = client;
                Client.OnChanged += ClientChanged;

                Client.Register<PeerStatusEvent>(IManagerEventHandler);
                Client.Register<NewChannelEvent>(IManagerEventHandler);
                Client.Register<NewStateEvent>(IManagerEventHandler);
                Client.Register<HangupEvent>(IManagerEventHandler);
                Client.Register<StatusEvent>(IManagerEventHandler);
                Client.Register<PeerEntryEvent>(IManagerEventHandler);

                // events queue and channels
                Client.Register<QueueCallerJoinEvent>(IManagerEventHandler);
                Client.Register<QueueCallerAbandonEvent>(IManagerEventHandler);
                Client.Register<QueueCallerLeaveEvent>(IManagerEventHandler);

                Client.Register<QueueMemberAddedEvent>(IManagerEventHandler);
                Client.Register<QueueMemberPauseEvent>(IManagerEventHandler);
                Client.Register<QueueMemberPenaltyEvent>(IManagerEventHandler);
                Client.Register<QueueMemberRemovedEvent>(IManagerEventHandler);
                Client.Register<QueueMemberRinginuseEvent>(IManagerEventHandler);
                Client.Register<QueueMemberStatusEvent>(IManagerEventHandler);

                Client.Register<QueueParamsEvent>(IManagerEventHandler);
                Client.Register<QueueMemberEvent>(IManagerEventHandler);
            }
        }

        public void IManagerEventHandler(string sender, IManagerEventFromAsterisk @event)
        {
            try
            {
                HashSet<string> cardKeys = new HashSet<string>();
                if (@event is IChannelEvent eventChannel)
                {
                    bool proccess = true;
                    if (ShouldIgnore)
                    {
                        var channel = new AsteriskChannel(eventChannel.Channel);
                        if (channel.Protocol == AsteriskChannelProtocol.LOCAL) 
                            proccess = false;
                    }

                    if (proccess)
                    {
                        var key = HandleEvent(this, eventChannel);
                        cardKeys.Add(key);
                    }
                }

                if (@event is IPeerStatus peerStatusEvent)
                    cardKeys.Add(HandleEvent(this, peerStatusEvent));

                if (@event is IQueueEvent eventQueue)                
                    cardKeys.Add(HandleEvent(this, eventQueue));

                //_logger.LogDebug($"event: {@event.GetType()}, cardKeys: {string.Join('|', cardKeys)}");


                // handling auto discover cards
                if (Options != null && Options.AutoFill)
                {
                    var newEvent = @event;
                    var cards = new HashSet<EventsPanelCard>();
                    foreach (var key in cardKeys)
                    {
                        //_logger.LogDebug($"card: {key}");
                        foreach (var card in HandleCard(key, newEvent))
                        {
                            //_logger.LogDebug($"card: {card.Label} :: { string.Join(" | ", card.Keys) }");
                            cards.Add(card);
                        }
                    }
                }

                /*
                foreach (var card in cards)
                    this.Event(card, newEvent);                    
                */


                OnEvent?.Invoke(cardKeys, @event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"error on receive event: { @event.GetType() }, from: { sender }");
            }
        }


        private void ClientChanged(HubConnectionState? state, Exception? ex)
            => OnChanged?.Invoke(state, ex);

        public EventsPanelServiceOptions? Options { get; internal set; }

        public void OnConfigure(EventsPanelServiceOptions? options)
        {
            _logger.LogDebug($"trying to parse options");
            if (options != null && !options.Equals(Options))
            {
                Options = options;
                if (Options.Cards.Any())
                {
                    foreach (var card in Options.Cards)
                    {                        
                        var cardMonitor = EventsPanelCardExtensions.CardCreate(card, this);
                        _cards.Add(cardMonitor);
                    }
                }

                Panel.Update(Options);
                _logger.LogInformation($"Configuração atualizada, Max Buttons: { Options.MaxButtons }, Cards: { Options.Cards.Count() }, ShowTrunks: { Options.ShowTrunks }");
            }
        }
        
        #region EVENT HANDLERS

        protected bool LimitReached => Options?.MaxButtons > 0 && _cards.Count >= Options.MaxButtons;

        protected bool ShouldFill => !LimitReached && (Options == null || Options.AutoFill);

        protected bool ShouldIgnore => Options == null || Options.IgnoreLocal;

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
        /// Return base cards generated;
        /// </summary>
        /// <returns></returns>
        public IEnumerable<EventsPanelCard> GetCards() => _cards;

        public bool Append(EventsPanelCard card)
        {
            if (!_cards.Contains(card))
            {
                _cards.Add(card);
                return true;
            }
            return false;
        }

        public bool IsConnected => Client?.State == HubConnectionState.Connected;

        public bool IsTrying => Client?.State == HubConnectionState.Connecting || Client?.State == HubConnectionState.Reconnecting;


        public HubConnectionState? State => Client?.State;

        public Panel Panel { get; }

        public async Task GetPeerStatus(CancellationToken cancellationToken = default)
        {
            if (Client != null && Client.State == HubConnectionState.Connected)
                await Client.GetPeerStatus(cancellationToken);
        }

        public async Task GetQueueStatus(string queue, string member, CancellationToken cancellationToken = default)
        {
            if (Client != null && Client.State == HubConnectionState.Connected)
                await Client.GetQueueStatus(queue, member, cancellationToken);
        }

        public delegate Task<string> AsyncTaskMonitor(EventsPanelCard monitor);

        public Func<EventsPanelCard, Task<string>>? CardAvatarHandler { get; set; }

        /// <summary>
        /// IMonitor Key, only representative, not used.
        /// </summary>
        public string Key => nameof(EventsPanelService);
    }
}
