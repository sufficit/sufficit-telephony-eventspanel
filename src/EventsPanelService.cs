using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sufficit.Asterisk;
using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Sufficit.Telephony.EventsPanel.IMonitor;

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

            _logger = _provider.GetRequiredService<ILogger<EventsPanelService>>();
            var client = _provider.GetService<AMIHubClient>();
            if(client != null)
            {
                Configure(client);
            }

            Panel = new Panel(_cards);
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
            if (OnEvent != null)
            {
                try
                {
                    OnEvent.Invoke(this, @event);
                }
                catch { }
            }

            try
            {
                HashSet<string> cardKeys = new HashSet<string>();
                if (@event is IChannelEvent eventChannel)
                {
                    if (ShouldIgnore)
                    {
                        var channel = new AsteriskChannel(eventChannel.Channel);
                        if (channel.Protocol == AsteriskChannelProtocol.LOCAL) return;
                    }

                    var key = HandleEvent(this, eventChannel);
                    cardKeys.Add(key);
                }

                if (@event is IPeerStatus peerStatusEvent)
                    cardKeys.Add(HandleEvent(this, peerStatusEvent));

                if (@event is IQueueEvent eventQueue)                
                    cardKeys.Add(HandleEvent(this, eventQueue));

                // _logger.LogDebug($"event: {@event.GetType()}, cardKeys: {string.Join('|', cardKeys)}");

                
                // handling auto discover cards
                var newEvent = @event;
                var cards = new HashSet<EventsPanelCard>();
                foreach (var key in cardKeys)
                    foreach (var card in HandleCard(key, newEvent))
                        cards.Add(card);

                foreach (var card in cards)
                    this.Event(card, newEvent);                    
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"error on receive event: { @event.GetType() }, from: { sender }");
            }
        }

        public event EventHandler<IManagerEventFromAsterisk>? OnEvent;

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
                    _cards.Clear();
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
                if (_client.State == HubConnectionState.Disconnected)
                {
                    _logger.LogInformation("starting hosted service");
                    try
                    {
                        await _client.StartAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        OnChanged?.Invoke(_client.State, ex);
                    }
                }
                else
                {
                    _logger.LogDebug($"starting hosted service notice status: { _client.State.ToString() }");
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

        protected async Task<IEnumerable<EventsPanelCard>> HandleCardAsync(string key, IManagerEvent eventObj)
            => await Task.Run(() => HandleCard(key, eventObj));

        protected IEnumerable<EventsPanelCard> HandleCard(string key, IManagerEvent eventObj)
        {
            var cards = HandleCardByKey(key);
            if (cards.Any()) return cards;
                      
            if (!ShouldFill) return Array.Empty<EventsPanelCard>();

            var cardMonitor = eventObj.ToCard(this);

            //ignoring peers auto fill
            if (!ShouldFillPeers && cardMonitor.Info.Kind == EventsPanelCardKind.PEER)
                return Array.Empty<EventsPanelCard>();

            if (!ShouldFillQueues && cardMonitor.Info.Kind == EventsPanelCardKind.QUEUE)
                return Array.Empty<EventsPanelCard>();

            _cards.Add(cardMonitor); // include global

            return new[] { cardMonitor };
        }

        #endregion        

        public bool IsConnected => _client?.State == HubConnectionState.Connected;

        public bool IsTrying => _client?.State == HubConnectionState.Connecting || _client?.State == HubConnectionState.Reconnecting;

        public bool IsConfigured => _client != null;

        public HubConnectionState? State => _client?.State;

        public Panel Panel { get; }

        /// <summary>
        /// On Status Changed or Exception occurs
        /// </summary>
        public event Action<HubConnectionState?, Exception?>? OnChanged;

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
