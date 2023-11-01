using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sufficit.Asterisk;
using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Specialized;

namespace Sufficit.Telephony.EventsPanel
{
    public partial class EventsPanelService : BackgroundService, IEventsPanelService, IHealthCheck
    {
        #region IMPLEMENT HEALTH CHECK

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
        {
            if (ExecuteTask == null)
                return Task.FromResult(HealthCheckResult.Unhealthy("not started or disposed"));

            if (ExecuteTask.Status != TaskStatus.WaitingForActivation && ExecuteTask.Status != TaskStatus.Running)
                return Task.FromResult(HealthCheckResult.Unhealthy($"status not running: {ExecuteTask.Status}"));

            return Task.FromResult(HealthCheckResult.Healthy("A healthy result."));
        }

        #endregion
    
        #region IMPLEMENT INTERFACE EVENTSPANEL SERVICE

        public bool IsConfigured => _client != null;

        public const int DELAYMILLISECONDS = 30000;

        Task IEventsPanelService.ExecuteAsync(CancellationToken stoppingToken) => ExecuteAsync(stoppingToken);

        private CancellationTokenSource? _cts;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);            
            do
            {
                if (_client != null)
                {
                    if (_client.State == HubConnectionState.Disconnected)
                    {
                        try
                        {
                            await _client.StartAsync(_cts.Token);
                            _logger.LogInformation("client state is: {state}", _client.State);

                            // awaiting infinite until cancellation triggered
                            await Task.Delay(Timeout.Infinite, _cts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogInformation("executing operation canceled");
                            // its stops _client 
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "error on starting client, trying again in {time} milliseconds", DELAYMILLISECONDS);
                            _ = await Delay(_cts.Token);
                        }
                    }
                } 
                else
                {
                    _logger.LogTrace("null client, not configured yet, trying again in {time} milliseconds", DELAYMILLISECONDS);
                }
            } while (await Delay(_cts.Token));

            if (_client != null && _client.State == HubConnectionState.Connected)
                await _client.StopAsync(CancellationToken.None);            
        }

        private async Task<bool> Delay(CancellationToken cancellationToken)
        {
            if(cancellationToken.IsCancellationRequested) 
                return false;

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(DELAYMILLISECONDS), cancellationToken);
                return true;
            }
            catch (OperationCanceledException) { return false; }
        }

        #endregion

        private readonly IDisposable? _optionsMonitor;
        private readonly ILogger _logger;
        private readonly IEventsPanelCardCollection _cards;
        private readonly IServiceProvider _provider;

        private AMIHubClient? _client;

        public ChannelInfoCollection Channels { get; }

        public PeerInfoCollection Peers { get; }

        public QueueInfoCollection Queues { get; }

        public ICollection<Exception> Exceptions { get; }

        public Uri? EndPoint =>
            _client?.Options?.Endpoint;

        /// <inheritdoc cref="AMIHubClient.OnChanged"/>
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
            if (client != null)
                Configure(client);             

            Panel = new Panel(_cards, this);

            var monitor = _provider.GetRequiredService<IOptionsMonitor<EventsPanelServiceOptions>>();
            _optionsMonitor = monitor.OnChange(Configure);            

            _logger.LogTrace($"Serviço de Controle { GetType().Name } construído !");
        }

        public void Configure(EventsPanelServiceOptions options)
        {
            if (!options.Equals(Options))
            {
                Options = options;
                _logger.LogInformation($"options updated, max buttons: {Options.MaxButtons}, cards: {Options.Cards.Count()}, show trunks: {Options.ShowTrunks}");

                if (Options.Cards.Any())
                {
                    foreach (var card in Options.Cards)
                    {
                        var cardMonitor = EventsPanelCardExtensions.CardCreate(card, this);
                        _cards.Add(cardMonitor);
                    }
                }

                Panel.Update(Options);              
            }
        }

        public async void Configure(AMIHubClient client)
        {
            if (client != null && !client.Equals(_client))
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
            _logger.LogTrace("event: {type}, from: {sender}", @event.GetType(), sender);
            try
            {
                var cardKeys = new HashSet<string>();
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

        public void ChannelsCleanUp()
        {
            foreach(var item in Channels.ToList())
            {
                if (item.LastUpdate.AddMinutes(20) < DateTime.UtcNow)
                {
                    Channels.Remove(item);
                }
            }
        }

        private void ClientChanged(HubConnectionState? state, Exception? ex)
            => OnChanged?.Invoke(state, ex);

        public EventsPanelServiceOptions? Options { get; internal set; }
                        
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

        public bool IsConnected => _client?.State == HubConnectionState.Connected;

        public bool IsTrying => _client?.State == HubConnectionState.Connecting || _client?.State == HubConnectionState.Reconnecting;

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
        ///     IMonitor Key, only representative, not used.
        /// </summary>
        public string Key => nameof(EventsPanelService);
    }
}
