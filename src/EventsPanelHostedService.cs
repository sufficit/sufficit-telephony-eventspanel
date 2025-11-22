using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    /// <summary>
    /// Hosted service that starts and manages the EventsPanelService lifecycle
    /// Ensures the service starts automatically when the application starts
    /// </summary>
public class EventsPanelHostedService : IHostedService, IDisposable
    {

        #region PRIVATE FIELDS

        private readonly IEventsPanelService _eventsPanelService;
        private readonly ILogger<EventsPanelHostedService> _logger;
      private CancellationTokenSource? _cancellationTokenSource;
        private Task? _executeTask;

      #endregion
        #region CONSTRUCTOR

        public EventsPanelHostedService(
  IEventsPanelService eventsPanelService,
         ILogger<EventsPanelHostedService> logger)
{
            _eventsPanelService = eventsPanelService;
            _logger = logger;
        }

   #endregion
        #region HOSTED SERVICE IMPLEMENTATION

  public Task StartAsync(CancellationToken cancellationToken)
        {
     _logger.LogInformation("EventsPanelHostedService is starting");

         _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

 // Start the background service
    _executeTask = _eventsPanelService.ExecuteAsync(_cancellationTokenSource.Token);

    if (_executeTask.IsCompleted)
         {
       return _executeTask;
            }

        _logger.LogInformation("EventsPanelHostedService started successfully");
         return Task.CompletedTask;
        }

   public async Task StopAsync(CancellationToken cancellationToken)
      {
            _logger.LogInformation("EventsPanelHostedService is stopping");

  if (_executeTask == null)
            {
     return;
        }

   try
            {
          // Signal cancellation to the executing method
       _cancellationTokenSource?.Cancel();
   }
    finally
 {
           // Wait until the task completes or the stop token triggers
    await Task.WhenAny(_executeTask, Task.Delay(Timeout.Infinite, cancellationToken));
      }

   _logger.LogInformation("EventsPanelHostedService stopped");
        }

        #endregion
        #region DISPOSE

        public void Dispose()
      {
      _cancellationTokenSource?.Cancel();
_cancellationTokenSource?.Dispose();
        }

        #endregion

    }
}
