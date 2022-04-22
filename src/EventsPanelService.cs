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

namespace Sufficit.Telephony.EventsPanel
{
    public class EventsPanelService : IHostedService
    {
        private readonly ILogger _logger;
        private AMIHubClient? _client;
        private readonly IEventsPanelCardCollection _cards;
        private readonly IServiceProvider _provider;

        public ICollection<Exception> Exceptions { get; }

        public EventsPanelService(IServiceProvider provider)
        {
            Exceptions = new List<Exception>();
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

        public void Configure(AMIHubClient client)
        {
            if(client != null && !client.Equals(_client))
            {
                _client = client;
                _client.OnChanged += _client_OnChanged;

                _client.Register<AMIPeerStatusEvent>(PeerStatusEventHandler);
                _client.Register<AMINewChannelEvent>(NewChannelEventHandler);
                _client.Register<AMINewStateEvent>(NewStateEventHandler);
                _client.Register<AMIHangupEvent>(HangupEventHandler);
                _client.Register<StatusEvent>(StatusEventHandler);
                _client.Register<PeerEntryEvent>(PeerEntryEventHandler);
            }
        }

        private async void _client_OnChanged(AMIHubClient _)
        {
            if(OnChanged != null)
                await OnChanged.Invoke();
        }

        public EventsPanelServiceOptions? Options { get; internal set; }

        public void OnConfigure(EventsPanelServiceOptions? options)
        {
            if (options != null && !options.Equals(Options))
            {
                Options = options;
                if (Options.Cards.Any())
                {
                    Panel.Cards.Clear();
                    foreach (var card in Options.Cards)
                        Panel.Cards.Add(new EventsPanelCardMonitor(card));                    
                }


                _logger.LogInformation($"Configuração atualizada, Max Buttons: { Options.MaxButtons }");
            }
        }

        #region IMPLEMENTAÇÃO DA INTERFACE IHOSTED SERVICE

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if(_client?.State == HubConnectionState.Disconnected)
                await _client.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        #endregion
        #region EVENT HANDLERS

        protected bool LimitReached => Options?.MaxButtons > 0 && _cards.Count >= Options.MaxButtons;

        protected bool ShouldFill => !LimitReached && (Options == null || Options.AutoFill);

        protected bool ShouldIgnore => Options == null || Options.IgnoreLocal;

        protected IEnumerable<EventsPanelCardMonitor> HandleCardByKey(string key)
        {
            int count = 0;
            foreach (var card in _cards[key])
            {
                count++;
                yield return card;
                if (card.Exclusive) yield break;
            }
        }

        protected IEnumerable<EventsPanelCardMonitor> HandlerCard(string key, AMIPeerStatusEvent eventObj)
        {
            var cards = HandleCardByKey(key);
            if (cards.Any()) return cards;
                      
            if (!ShouldFill) return Array.Empty<EventsPanelCardMonitor>();

            var card = eventObj.ToCard();
            _cards.AddCard(card); // include global

            return new[] { card };        
        }
        protected IEnumerable<EventsPanelCardMonitor> HandlerCard(string key, IChannelEvent eventObj)
        {
            var cards = HandleCardByKey(key);
            if (cards.Any()) return cards;

            if (!ShouldFill) return Array.Empty<EventsPanelCardMonitor>();

            var card = eventObj.ToCard();
            _cards.AddCard(card); // include global

            return new[] { card };
        }

        protected IEnumerable<EventsPanelCardMonitor> HandlerCard(string key, PeerEntryEvent eventObj)
        {
            var cards = HandleCardByKey(key);
            if (cards.Any()) return cards;

            if (!ShouldFill) return Array.Empty<EventsPanelCardMonitor>();

            var card = eventObj.ToCard();
            _cards.AddCard(card); // include global

            return new[] { card };
        }

        protected async Task PeerEntryEventHandler(string sender, PeerEntryEvent eventObj)
        {
            await Task.Yield();
            var key = eventObj.GetPeerInfo().GetDial();
            foreach (var card in HandlerCard(key, eventObj))
                card.Event(eventObj);
        }

        protected async Task StatusEventHandler(string sender, StatusEvent eventObj)
        {
            await Task.Yield(); 
            if (ShouldIgnore)
            {
                var channel = new AsteriskChannel(eventObj.Channel);
                if (channel.Protocol == AsteriskChannelProtocol.LOCAL) return;
            }

            foreach (var card in HandlerCard(eventObj.Channel, eventObj))
                card.Event(eventObj);
        }

        protected async Task PeerStatusEventHandler(string sender, AMIPeerStatusEvent eventObj)
        {
            await Task.Yield();
            foreach (var card in HandlerCard(eventObj.Peer, eventObj))            
                card.Event(eventObj);
        }

        protected async Task NewChannelEventHandler(string sender, AMINewChannelEvent eventObj)
        {
            await Task.Yield();
            if (ShouldIgnore)
            {
                var channel = new AsteriskChannel(eventObj.Channel);
                if (channel.Protocol == AsteriskChannelProtocol.LOCAL) return;
            }

            foreach (var card in HandlerCard(eventObj.Channel, eventObj))            
                card.Event(eventObj);            
        }

        protected async Task NewStateEventHandler(string sender, AMINewStateEvent eventObj)
        {
            await Task.Yield();
            if (ShouldIgnore)
            {
                var channel = new AsteriskChannel(eventObj.Channel);
                if (channel.Protocol == AsteriskChannelProtocol.LOCAL) return;
            }

            foreach (var card in HandlerCard(eventObj.Channel, eventObj))
                card.Event(eventObj);
        }

        protected async Task HangupEventHandler(string sender, AMIHangupEvent eventObj)
        {
            await Task.Yield();
            if (ShouldIgnore)
            {
                var channel = new AsteriskChannel(eventObj.Channel);
                if (channel.Protocol == AsteriskChannelProtocol.LOCAL) return;
            }

            foreach (var card in HandlerCard(eventObj.Channel, eventObj))
                card.Event(eventObj);
        }

        #endregion


        public bool IsConnected => _client?.State == HubConnectionState.Connected;

        public bool IsTrying => _client?.State == HubConnectionState.Connecting || _client?.State == HubConnectionState.Reconnecting;

        public bool IsConfigured => _client != null;

        public HubConnectionState? State => _client?.State;

        public Panel Panel { get; }

        public event AsyncEventHandler? OnChanged;

        public delegate Task AsyncEventHandler();

        public async Task GetPeerStatus(CancellationToken cancellationToken = default)
        {
            if (_client != null && _client.State == HubConnectionState.Connected)
                await _client.GetPeerStatus(cancellationToken);
        }

        public delegate Task<string> AsyncTaskMonitor(EventsPanelCardMonitor monitor);

        public Func<EventsPanelCardMonitor, Task<string>>? CardAvatarHandler { get; set; }
    }
}
