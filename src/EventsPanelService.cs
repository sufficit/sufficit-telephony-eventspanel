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
    public partial class EventsPanelService : IHostedService
    {
        private readonly ILogger _logger;
        private AMIHubClient? _client;
        private readonly IEventsPanelCardCollection _cards;
        private readonly IServiceProvider _provider;

        public ChannelInfoCollection Channels { get; }
        public PeerInfoCollection Peers { get; }
        public QueueInfoCollection Queues { get; }
        public ICollection<Exception> Exceptions { get; }

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
            var client = _provider.GetService<AMIHubClient>();
            if(client != null)
            {
                Configure(client);
            }

            Panel = new Panel(_cards, this);
            var monitor = _provider.GetService<IOptionsMonitor<EventsPanelServiceOptions>>();
            OnConfigure(monitor?.CurrentValue);
            monitor.OnChange(OnConfigure);                        

            _logger.LogTrace($"Serviço de Controle { GetType().Name } construído !");
        }
               
        public async void Configure(AMIHubClient client)
        {
            if(client != null && !client.Equals(_client))
            {
                if(_client != null)                
                    await _client.DisposeAsync();                

                _client = client;
                _client.OnChanged += ClientChanged;

                _client.Register<PeerStatusEvent>(IManagerEventHandler);
                _client.Register<NewChannelEvent>(IManagerEventHandler);
                _client.Register<NewStateEvent>(IManagerEventHandler);
                _client.Register<HangupEvent>(IManagerEventHandler);
                _client.Register<StatusEvent>(IManagerEventHandler);
                _client.Register<PeerEntryEvent>(IManagerEventHandler);

                // events queue and channels
                _client.Register<QueueCallerJoinEvent>(IManagerEventHandler);
                _client.Register<QueueCallerAbandonEvent>(IManagerEventHandler);
                _client.Register<QueueCallerLeaveEvent>(IManagerEventHandler);

                _client.Register<QueueMemberAddedEvent>(IManagerEventHandler);
                _client.Register<QueueMemberPauseEvent>(IManagerEventHandler);
                _client.Register<QueueMemberPenaltyEvent>(IManagerEventHandler);
                _client.Register<QueueMemberRemovedEvent>(IManagerEventHandler);
                _client.Register<QueueMemberRinginuseEvent>(IManagerEventHandler);
                _client.Register<QueueMemberStatusEvent>(IManagerEventHandler);

                _client.Register<QueueParamsEvent>(IManagerEventHandler);
                _client.Register<QueueMemberEvent>(IManagerEventHandler);
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

        #region IMPLEMENTAÇÃO DA INTERFACE IHOSTED SERVICE

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_client != null) 
            { 
                if (!_client.State.HasValue || _client.State == HubConnectionState.Disconnected)
                {
                    _logger.LogInformation("starting hosted service");
                    try
                    {
                        await _client.StartAsync(cancellationToken); 
                        OnChanged?.Invoke(_client.State, null);
                    }
                    catch (Exception ex)
                    {
                        OnChanged?.Invoke(_client.State, ex);
                    }
                }
                else
                {
                    _logger.LogDebug($"starting hosted service notice status: { _client?.State }");
                }                 
            }
            else
            {
                _logger.LogWarning("starting hosted service fail: hub client null");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Queued Hosted Service is stopping.");
            if(_client != null)
            {
                await _client.DisposeAsync();
            }
        }

        #endregion
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

        public bool IsConnected => _client?.State == HubConnectionState.Connected;

        public bool IsTrying => _client?.State == HubConnectionState.Connecting || _client?.State == HubConnectionState.Reconnecting;

        public bool IsConfigured => _client != null;

        public HubConnectionState? State => _client?.State;

        public Panel Panel { get; }


        public async Task GetPeerStatus(CancellationToken cancellationToken = default)
        {
            if (_client != null && _client.State == HubConnectionState.Connected)
                await _client.GetPeerStatus(cancellationToken);
        }

        public async Task GetQueueStatus(string queue, string member, CancellationToken cancellationToken = default)
        {
            if (_client != null && _client.State == HubConnectionState.Connected)
                await _client.GetQueueStatus(queue, member, cancellationToken);
        }

        public delegate Task<string> AsyncTaskMonitor(EventsPanelCard monitor);

        public Func<EventsPanelCard, Task<string>>? CardAvatarHandler { get; set; }

        /// <summary>
        /// IMonitor Key, only representative, not used.
        /// </summary>
        public string Key => nameof(EventsPanelService);
    }
}
