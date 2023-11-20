using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sufficit.Telephony.EventsPanel
{
    public class MonitorCollection<T> : ICollection<T> where T : IMonitor
    {
        /// <summary>
        ///     Used for thread safe change the collection
        /// </summary>
        public object KeysLock => _lockKeys;

        private readonly IDictionary<string, T> _items;
        private readonly object _lockKeys;
        private readonly object _lockValues;

        public MonitorCollection()
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            _items = new Dictionary<string, T>(comparer);

            _lockKeys = new object();
            _lockValues = new object();
        }

        ~MonitorCollection()
        {
            lock (_lockKeys)
                lock(_lockValues)
                    _items.Clear();
        }

        private event Action<T?, object?>? _onChanged;

        /// <summary>
        /// Monitor changes in the collection, numeric changes, add, remove, etc <br />
        /// Not internal items changes
        /// </summary>
        public event Action<T?, object?>? OnChanged
        {
            add { if(!IsEventHandlerRegistered(value)) _onChanged += value; }
            remove { _onChanged -= value; }
        }

        protected void Changed(T? monitor)
        {
            if(_onChanged != null)
            {
                _onChanged.Invoke(monitor, null);
            }
        }

        public bool IsEventHandlerRegistered(Delegate? prospectiveHandler)
        {
            if (_onChanged != null)
            {
                foreach (Delegate existingHandler in _onChanged.GetInvocationList())
                {
                    if (existingHandler == prospectiveHandler)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public virtual T? this[string key]
        {
            get
            {
                lock (_lockKeys)
                {
                    if (_items.ContainsKey(key))
                    {
                        lock (_lockValues)
                        {
                            return _items[key];
                        }
                    }
                }
                return default;
            }
        }

        #region BASIC LIST METHODS

        public virtual bool Contains(string key)
        {
            lock (_lockKeys)
            {
                return _items.ContainsKey(key);
            }
        }

        public virtual bool Contains(T monitor) 
            => Contains(monitor.Key);

        public virtual void Add(T monitor)
        {
            bool updated = false;
            lock (_lockKeys)
            {
                if (!_items.ContainsKey(monitor.Key))
                {
                    lock (_lockValues)
                    {
                        _items.Add(monitor.Key, monitor);
                        monitor.OnChanged += ItemChanged;
                        updated = true;
                    }
                }
            }

            // Trigering collection changed
            if (updated)
                Changed(monitor);
        }

        public virtual bool Remove(string key)
        {            
            var item = this[key];
            if(item != null)
                Remove(item);
            
            return false;
        }

        public virtual bool Remove(T monitor)
        {
            lock (_lockKeys)
            {
                lock (_lockValues)
                {
                    if (_items.Remove(monitor.Key))
                    {
                        monitor.OnChanged -= ItemChanged;

                        // Trigering collection changed
                        Changed(monitor);
                        return true;
                    }                
                }
            }
            return false;
        }

        public virtual IList<T> ToList()
        {
            lock (_lockValues)
            {
                return _items.Values.ToList();
            }
        }

        public virtual IList<CustomT> ToList<CustomT>()
        {
            lock (_lockValues)
            {
                return _items.Values.OfType<CustomT>().ToList();
            }
        }

        public virtual void Clear()
        {
            lock (_lockKeys)
            {
                lock (_lockValues)
                {
                    _items.Clear();

                    // Trigering collection changed
                    Changed(default);
                }
            }
        }
        public int Count => _items.Count;

        public bool IsReadOnly => _items.IsReadOnly;

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_lockKeys)
            {
                lock (_lockValues)
                {
                    _items.Values.CopyTo(array, arrayIndex);
                }
            }
        }


        #endregion

        /// <summary>
        /// On indivual internal item changed
        /// </summary>
        protected virtual void ItemChanged(IMonitor sender, object? state) 
        {
            // used to override
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (_lockKeys)
            {
                lock (_lockValues)
                {
                    return _items.Values.ToList().GetEnumerator();
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
