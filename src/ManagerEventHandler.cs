using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class ManagerEventHandler : IDisposable 
    {
        private readonly ManagerEventHandlerCollection _collection;

        public IDisposable? Disposable { internal get; set; }

        public string Key { get; }

        public Func<object[], object, Task> Action { get; internal set; } = default!;

        public Type[] Types { get; internal set; } = default!;

        public object State { get; internal set; } = default!;

        public ManagerEventHandler (ManagerEventHandlerCollection collection, string key) 
        {
            _collection = collection;
            Key = key;
        }

        public ManagerEventHandler Parse<T>(Action<string, T> action)
        {
            Types = new[] { typeof(string), typeof(T) };
            State = action;
            Action = static (parameters, state) =>
            {
                if (state is Action<string, T> currentHandler)
                    currentHandler((string)parameters[0], (T)parameters[1]);
                else
                    throw new Exception($"invalid state: {state}");
                return Task.CompletedTask;
            };
            return this;
        }

        public ManagerEventHandler Parse<T>(Func<string, T, Task> action)
        {
            Types = new[] { typeof(string), typeof(T) };
            State = action;
            Action = static (parameters, state) =>
            {
                if (state is Func<string, T, Task> currentHandler)                
                    return currentHandler((string)parameters[0], (T)parameters[1]);
                else
                    throw new Exception($"invalid state: {state}");
            };
            return this;
        }

        public void Dispose() 
        {
            Console.WriteLine($"---------------------- DISPOSED HANDLER ({Key}) -------------------------");
            _collection.UnRegister(this);
            Disposable?.Dispose(); 
        }

        public bool IsMatch<T>(string key, Action<string, T> state)
            => Key.Equals(key) && State.Equals(state);

        public bool IsMatch<T>(string key, Func<string, T, Task> state)
            => Key.Equals(key) && State.Equals(state);
    }
}
