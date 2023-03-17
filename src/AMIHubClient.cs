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
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class AMIHubClient : BackgroundService, IDisposable
    {
        /// <summary>
        /// Method name for system messages
        /// </summary>
        public const string SYSTEM = "System";

        private readonly ManagerEventHandlerCollection _handlers;
        private readonly ILogger _logger;
        private HubConnection? _hub;
        private CancellationTokenSource? _cts;
        private AMIHubClientOptions? _options;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                if (EnsureValidHub())
                {
                    if (_hub!.State == HubConnectionState.Disconnected)
                    {
                        try
                        {
                            await _hub.StartAsync(_cts.Token);
                            _logger.LogInformation("hub state is: {state}", _hub.State);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "error on connecting hub, trying again in 10 seconds");
                            await Task.Delay(TimeSpan.FromSeconds(10));
                        }
                    }
                    else
                    {
                        // yield to rotate again
                        await Task.Yield();
                    }
                } 
                else
                {
                    _cts.Cancel();
                }               
            }
        }

        protected bool EnsureValidHub()
        {
            if(_hub != null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Configure Hub Connection everytime that options changed
        /// </summary>
        protected async void Configure(AMIHubClientOptions options)
        {   
            var validate = options.Validate();
            if (validate != null)
            { 
                _logger.LogError(validate, "invalid options");
                return;
            }

            if (_options != null && _options.Equals(options))
                return;

            // updating last valid options
            _options = options;

            if (_hub != null)
            {
                HandlersClear(_hub);
                _hub.Remove(SYSTEM);
                await _hub.DisposeAsync().ConfigureAwait(false);
            }

            _logger.LogDebug("parsing options and creating hub");
            _hub = new HubConnectionBuilder()
                    .WithUrl(_options.Endpoint!, (opts) =>
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

            HandlersUpdate(_hub);
            RegisterHandlers(_hub);           
        }

        protected void HandlersUpdate(HubConnection hub)
        {
            hub.Closed          += _hub_Closed;
            hub.Reconnected     += _hub_Reconnected;
            hub.Reconnecting    += _hub_Reconnecting;
        }

        protected void HandlersClear(HubConnection hub)
        {
            hub.Closed          -= _hub_Closed;
            hub.Reconnected     -= _hub_Reconnected;
            hub.Reconnecting    -= _hub_Reconnecting;
        }
                        
        /// <summary>
        /// Default Singleton service provider constructor
        /// </summary>
        public AMIHubClient(IOptionsMonitor<AMIHubClientOptions> monitor, ILogger<AMIHubClient> logger)
        {
            _handlers = new ManagerEventHandlerCollection();
            _handlers.Registered += HanlderRegistered;

            _logger = logger;
            Configure(monitor.CurrentValue);
            monitor.OnChange(Configure);
        }

        /// <summary>
        /// New handler append to the internal collection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="handler"></param>
        private void HanlderRegistered(object? sender, ManagerEventHandler handler)
        {
            if (_hub == null) return;

            _logger.LogDebug($"registering by event key: {handler.Key}");
            handler.Disposable = _hub.On(handler.Key, handler.Types, handler.Action!, handler.State);
        }

        /// <summary>
        /// Ensure that the hub has all internal handlers registered
        /// </summary>
        /// <param name="hub"></param>
        protected void RegisterHandlers(HubConnection hub)
        {
            _logger.LogDebug($"loading internal handlers collection");
            hub.On<string>(SYSTEM, _hub_SystemMessage);
            foreach (var handler in _handlers)
                handler.Disposable = hub.On(handler.Key, handler.Types, handler.Action!, handler.State);
        }

        private void _hub_SystemMessage(string? message)
        {
            if (message != null)
                _logger.LogWarning($"{SYSTEM} message: {{message}}", message);

            OnChanged?.Invoke(State, null);
        }


        #region HUB STATE EVENTS

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

        private Task _hub_Closed(Exception? arg)
        {
            OnChanged?.Invoke(State, arg);
            return Task.CompletedTask;       
        }

        #endregion

        public IDisposable? Register<T>(Func<string, T, Task> action) where T : IManagerEvent
        {
            var key = typeof(T).Name;
            return _handlers.Handler(key, action);
        }

        public IDisposable? Register<T>(Action<string, T> action) where T : IManagerEvent
        {
            var key = typeof(T).Name;
            return _handlers.Handler(key, action);
        }











        public AMIHubClientOptions? Options => _options;

        public HubConnectionState? State => _hub?.State;

        public event Action<HubConnectionState?, Exception?>? OnChanged;

        public delegate void AsyncEventHandler(AMIHubClient sender);

        public async Task GetPeerStatus(CancellationToken cancellationToken = default)
        {
            if (_hub?.State == HubConnectionState.Connected)    
                await _hub.InvokeAsync("GetPeerStatus", cancellationToken);
        }

        public async Task GetQueueStatus(string queue, string member, CancellationToken cancellationToken = default)
        {
            if (_hub?.State == HubConnectionState.Connected)
                await _hub.InvokeAsync("GetQueueStatus", queue, member);
        }

        public override void Dispose()
        {
            _logger.LogWarning($"---------------------- DISPOSED AMI HUB CLIENT -------------------------");
        }

        private bool _disposed; // indicate that this object was disposed

        public async ValueTask DisposeAsync()
        {
            _logger.LogWarning($"---------------------- DISPOSED ASYNC AMI HUB CLIENT -------------------------");            
            if (!_disposed)
            {
                if (_hub != null)
                    await _hub.DisposeAsync();

                OnChanged = null;
                _disposed = true;
            }            
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HubConnection));
            }
        }
    }
}
