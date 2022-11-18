using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sufficit.Asterisk.Manager;
using Sufficit.Asterisk.Manager.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class AMIHubClient : IAsyncDisposable, IHostedService
    {
        private readonly ILogger<AMIHubClient> _logger;
        private readonly AMIHubClientOptions _options;
        private readonly HubConnection _hub;

        /// <summary>
        /// Default Singleton service provider constructor
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public AMIHubClient(IOptions<AMIHubClientOptions> options, ILogger<AMIHubClient> logger) : this(options.Value, logger) { }

        public AMIHubClient(AMIHubClientOptions options, ILogger<AMIHubClient> logger)
        {
            _logger = logger;
            _options = options;

            // creating hub
            _hub = Create(_options);
        }

        public AMIHubClient(AMIHubClientOptions options) : this(options, new LoggerFactory().CreateLogger<AMIHubClient>())
        {
            
        }

        protected HubConnection Create(AMIHubClientOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            options.ValidateAndThrow();
                
            _logger.LogDebug($"configuring ami hub to endpoint: {options.Endpoint}");
            var hub = new HubConnectionBuilder()
                    .WithUrl(options.Endpoint, (opts) =>
                    {
                        opts.HttpMessageHandlerFactory = (message) =>
                        {
                            if (message is HttpClientHandler clientHandler)
                                clientHandler.ServerCertificateCustomValidationCallback += (sender, certificate, chain, sslPolicyErrors) => { return true; };
                            return message;
                        };
                    })
                    .WithAutomaticReconnect()
                    .Build();

            hub.Closed += _hub_Closed;
            hub.Reconnected += _hub_Reconnected;
            hub.Reconnecting += _hub_Reconnecting;
            hub.On<string>("System", _hub_SystemMessage);
            return hub;
        }

        private void _hub_SystemMessage(string? message)
        {
            if (message != null)
                _logger.LogWarning(message);

            OnChanged?.Invoke(State, null);
        }

        private Task _hub_Reconnecting(Exception? arg)
        {
            OnChanged?.Invoke(State, arg);
            return Task.CompletedTask;
        }

        private Task _hub_Reconnected(string? arg)
        {
            OnChanged?.Invoke(State, null);
            return Task.CompletedTask;
        }

        #region HUB STATE EVENTS

        private Task _hub_Closed(Exception? arg)
        {
            OnChanged?.Invoke(State, arg);
            return Task.CompletedTask;       
        }

        #endregion

        public IDisposable? Register<T>(Func<string, T, Task> action) where T : IManagerEvent, new()
        {
            var key = typeof(T).Name;
            _logger.LogDebug($"Registering key: {key}");
            return _hub.On(key, action);
        }

        public IDisposable? Register<T>(Action<string, T> action) where T : IManagerEvent, new()
        {
            var key = typeof(T).Name;
            _logger.LogDebug($"Registering key: {key}");
            return _hub.On(key, action);
        }

        public AMIHubClientOptions Options => _options;

        public HubConnectionState? State => _hub.State;

        public event Action<HubConnectionState?, Exception?>? OnChanged;

        public delegate void AsyncEventHandler(AMIHubClient sender);

        public async Task GetPeerStatus(CancellationToken cancellationToken = default)
        {
            if (_hub.State == HubConnectionState.Connected)    
                await _hub.InvokeAsync("GetPeerStatus", cancellationToken);
        }

        public async Task GetQueueStatus(string queue, string member, CancellationToken cancellationToken = default)
        {
            if (_hub.State == HubConnectionState.Connected)
                await _hub.InvokeAsync("GetQueueStatus", queue, member);
        }

        public async ValueTask DisposeAsync()
        {
            await _hub.DisposeAsync();
            OnChanged = null;
        }

        #region IMPLEMENTAÇÃO DA INTERFACE IHOSTED SERVICE

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _hub.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _hub.StopAsync(cancellationToken);
        }

        #endregion
    }
}
