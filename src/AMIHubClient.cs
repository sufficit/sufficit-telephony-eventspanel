using Microsoft.AspNetCore.SignalR.Client;
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
    public class AMIHubClient : IAsyncDisposable
    {
        private readonly ILogger<AMIHubClient> _logger;
        private AMIHubClientOptions? _options;

        protected HubConnection? Hub { get; set; }

        public AMIHubClient(ILogger<AMIHubClient> logger, IOptionsMonitor<AMIHubClientOptions> monitor)
        {
            _logger = logger;

            var options = monitor.CurrentValue;
            if (options.Validate() == null)
                Configure(options);

            monitor.OnChange(Configure);           
        }

        public AMIHubClient(AMIHubClientOptions options, ILogger<AMIHubClient> logger)
        {
            if (logger != null) _logger = logger;
            else _logger = new LoggerFactory().CreateLogger<AMIHubClient>();

            Configure(options);
        }

        public void Configure(AMIHubClientOptions options)
        {
            if (options != null && options != _options)
            {                
                _options = options.ValidateAndThrow();

                var uri = _options.Endpoint;
                if (uri != null)
                {
                    Hub = new HubConnectionBuilder()
                        .WithUrl(uri)
                        .WithAutomaticReconnect()
                        .Build();
                    
                    Hub.Closed += _hub_Closed;
                    Hub.Reconnected += _hub_Reconnected;
                    Hub.Reconnecting += _hub_Reconnecting;
                    Hub.On<string>("System", _hub_SystemMessage);
                }
            }
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
            return Hub?.On(key, action);
        }

        public IDisposable? Register<T>(Action<string, T> action) where T : IManagerEvent, new()
        {
            var key = typeof(T).Name;
            _logger.LogDebug($"Registering key: {key}");
            return Hub?.On(key, action);
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (Hub != null)
                await Hub.StartAsync(cancellationToken);
        }

        public HubConnectionState? State => Hub?.State;

        public event Action<HubConnectionState?, Exception?>? OnChanged;

        public delegate void AsyncEventHandler(AMIHubClient sender);

        public async Task GetPeerStatus(CancellationToken cancellationToken = default)
        {
            if (Hub != null && Hub.State == HubConnectionState.Connected)    
                await Hub.InvokeAsync("GetPeerStatus", cancellationToken);
        }

        public async Task GetQueueStatus(string queue, string member, CancellationToken cancellationToken = default)
        {
            if (Hub != null && Hub.State == HubConnectionState.Connected)
                await Hub.InvokeAsync("GetQueueStatus", queue, member);
        }

        public async ValueTask DisposeAsync()
        {
            if(Hub != null)
                await Hub.DisposeAsync();

            OnChanged = null;
            _options = null;
        }
    }
}
