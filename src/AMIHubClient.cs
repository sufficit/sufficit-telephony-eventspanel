using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sufficit.Asterisk.Manager.Events;
using System;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Sufficit.Telephony.EventsPanel
{
    public class AMIHubClient : BackgroundService, IDisposable, IHealthCheck
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

        /// <summary>
        /// Method name for system messages
        /// </summary>
        public const string SYSTEM = "System";

        public const int DELAYMILLISECONDS = 30000;

        private readonly ManagerEventHandlerCollection _handlers;
        private readonly ILogger _logger;
        private readonly IDisposable? _monitor;
        private readonly int _instance;

        private HubConnection? _hub;
        private CancellationTokenSource? _cts;
        private AMIHubClientOptions? _options;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // cancelling previous token, if exists
            if (_cts != null) _cts.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            
            do
            {
                if (EnsureValidHub())
                {
                    if (_hub!.State == HubConnectionState.Disconnected)
                    {
                        try
                        {
                            await _hub.StartAsync(_cts.Token);
                            _logger.LogInformation("hub state is: {state}", _hub.State);

                            // invoking first changed events
                            OnChanged?.Invoke(_hub.State, null);

                            // awaiting infinite until cancellation triggered
                            await Task.Delay(Timeout.Infinite, _cts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogInformation("executing operation canceled");
                            await _hub.StopAsync(CancellationToken.None);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "error on connecting hub, trying again in {time} milliseconds", DELAYMILLISECONDS);
                            _ = await Delay(_cts.Token);
                        }
                    }
                } 
                else
                {
                    _logger.LogWarning("invalid options");
                    _cts.Cancel();
                }               
            } while (await Delay(_cts.Token));
        }

        private async Task<bool> Delay(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return false;

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(DELAYMILLISECONDS), cancellationToken);
                return true;
            }
            catch (OperationCanceledException) { return false; }
        }

        protected bool EnsureValidHub()
        {
            if(_hub != null)            
                return true;
            
            return false;
        }

        /// <summary>
        ///     Configure Hub Connection everytime that options changed
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

            if (_hub != null)
            {
                _logger.LogInformation("({instance}) disposing old hub with endpoint: {endpoint}", _instance, _options?.Endpoint);
                HandlersClear(_hub);
                _hub.Remove(SYSTEM);
                await _hub.DisposeAsync().ConfigureAwait(false);
            }

            // updating last valid options
            _options = options;

            _logger.LogTrace("({instance}) parsing options and creating hub with endpoint: {endpoint}", _instance, _options.Endpoint);
            _hub = new HubConnectionBuilder()
                .AddJsonProtocol(opts =>
                {
                    opts.PayloadSerializerOptions = Sufficit.Json.Options;
                })
                .WithUrl(_options.Endpoint!, (opts) =>
                {
                    opts.HttpMessageHandlerFactory = (message) =>
                    {
                        if (message is HttpClientHandler clientHandler)
                        {
                            // if not using any browser platform
                            var platform = OSPlatform.Create("browser");
                            if (!RuntimeInformation.IsOSPlatform(platform))
                            {
                                // do not check for certificates
                                clientHandler.ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => { return true; };
                            }
                        }
                        return message;
                    };
                })
                .WithAutomaticReconnect()
                .Build();
             
            //_hub.On("PeerStatusEvent", Log);
            //_ = _hub.On<string, JsonElement>("PeerStatusEvent", (server, message) => Console.WriteLine("event received: {0}, {1}", server, message));
            
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
        ///     Default Singleton service provider constructor
        /// </summary>
        public AMIHubClient(IOptionsMonitor<AMIHubClientOptions> monitor, ILogger<AMIHubClient> logger)
        {
            _instance = new Random().Next(0, 100);
            _handlers = new ManagerEventHandlerCollection();
            _handlers.Registered += HandlerRegistered;

            _logger = logger;
            Configure(monitor.CurrentValue);
            _monitor = monitor.OnChange(Configure);
        }

        /// <summary>
        ///     New handler append to the internal collection
        /// </summary>
        private void HandlerRegistered(object? sender, ManagerEventHandler handler)
        {
            if (_hub == null) throw new Exception("null hub");

            _logger.LogTrace("registering by event key: {0}, types: {0}", handler.Key, handler.Types);
            handler.Disposable = _hub.On(handler.Key, handler.Types, handler.Action!, handler.State);
        }

        /// <summary>
        ///     Ensure that the hub has all internal handlers registered
        /// </summary>
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
                _logger.LogWarning($"({SYSTEM}) message: {{message}}", message);

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

        public IDisposable Register<T>(Func<string, T, Task> action) where T : IManagerEvent
        {
            var key = typeof(T).Name;
            return _handlers.Handler(key, action);
        }

        public IDisposable Register<T>(Action<string, T> action) where T : IManagerEvent
        {
            var key = typeof(T).Name;
            return _handlers.Handler(key, action);
        }

        public AMIHubClientOptions? Options => _options;

        public HubConnectionState? State => _hub?.State;

        /// <summary>
        ///     On Status Changed or Exception occurs
        /// </summary>
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
                await _hub.InvokeAsync("GetQueueStatus", queue, member, cancellationToken);
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

                // disposing options monitor
                _monitor?.Dispose();
                                
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
