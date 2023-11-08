using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class ManagerEventHandlerCollection : IEnumerable<ManagerEventHandler>
    {
        private readonly HashSet<ManagerEventHandler> _items;

        public ManagerEventHandlerCollection() { 
            _items = new HashSet<ManagerEventHandler>(); 
        }

        public event EventHandler<ManagerEventHandler>? Registered;

        public event EventHandler<ManagerEventHandler>? UnRegistered;        

        public ManagerEventHandler Handler<T>(string key, Action<string, T> state)
        {
            var handler = this.FirstOrDefault(s => s.IsMatch(key, state));
            if (handler == null)
            {
                handler = new ManagerEventHandler(this, key).Parse(state);
                if (_items.Add(handler))
                    Registered?.Invoke(this, handler);
            }
            return handler;
        }

        public ManagerEventHandler Handler<T>(string key, Func<string, T, Task> state)
        {
            var handler = this.FirstOrDefault(s => s.IsMatch(key, state));
            if (handler == null)
            {
                handler = new ManagerEventHandler(this, key).Parse(state);
                if (_items.Add(handler))                
                    Registered?.Invoke(this, handler);                
            }
            return handler;
        }

        public void UnRegister(ManagerEventHandler handler)
        {
            if (_items.Remove(handler))            
                UnRegistered?.Invoke(this, handler);            
        }

        public IEnumerator<ManagerEventHandler> GetEnumerator()
            => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => _items.GetEnumerator();
    }
}
