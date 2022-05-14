using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public interface IBackgroundTaskQueue
    {
        ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem);

        ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(
            CancellationToken cancellationToken);
    }

    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<Func<CancellationToken, ValueTask>> _queue;

        public BackgroundTaskQueue(int capacity)
        {
            // Capacity should be set based on the expected application load and
            // number of concurrent threads accessing the queue.            
            // BoundedChannelFullMode.Wait will cause calls to WriteAsync() to return a task,
            // which completes only when space became available. This leads to backpressure,
            // in case too many publishers/calls start accumulating.
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
        }

        public async ValueTask QueueBackgroundWorkItemAsync(
            Func<CancellationToken, ValueTask> workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            await _queue.Writer.WriteAsync(workItem);
        }

        public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(
            CancellationToken cancellationToken)
        {
            var workItem = await _queue.Reader.ReadAsync(cancellationToken);

            return workItem;
        }
    }
    public class TaskQueue
    {
        public int Count => queue.Count;
        public int Processed { get; internal set; }

        private readonly object _lock;
        private Queue<Action> queue;
        private SemaphoreSlim semaphore;

        public TaskQueue()
        {
            _lock = new object();
            queue = new Queue<Action>();
            semaphore = new SemaphoreSlim(1);
        }

        public void Enqueue(Action taskGenerator)
        {
            queue.Enqueue(taskGenerator); 
            Worker();
        }

        protected bool working = false;
        protected async void Worker()
        {
            lock (_lock)
            {
                if (working) return;
                working = true;
            }

            while (queue.TryDequeue(out Action? task))
                await Process(task);

            working = false;
        }

        protected async Task Process(Action taskGenerator)
        {
            await semaphore.WaitAsync();
            try
            {                
                await Task.Factory.StartNew(taskGenerator, TaskCreationOptions.LongRunning);
                Processed++;   
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
