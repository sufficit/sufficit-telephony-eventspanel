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
    /// <summary>
    /// Collection using HashTable to improve results on single thread applications
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CardCollection<T> : IEnumerable<T>, ICollection<T> where T : IMultipleKey
    {
        private readonly Hashtable _items;
        private readonly object? _lock;

        public CardCollection(object? threadLock = default)
        {
            _items = new Hashtable();
            _lock = threadLock;
        }

        ~CardCollection()
        {            
            lock (_lock ?? new object())
                _items.Clear();            
        }
        
        /// <summary>
        /// Fastest method to retrieve an item
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public T? GetItem(string[] keys)
        {
            foreach (string key in keys)
                if (_items.ContainsKey(key))
                    return (T?)_items[key];

            return default(T?);
        }

        public virtual T? this[string key]
        {
            get
            {
                if (_items.ContainsKey(key))
                    return (T?)_items[key];

                return default(T?);
            }
        }

        #region ON CHANGE EVENTS

        private event Action<T?, NotifyCollectionChangedAction>? _onChanged;

        /// <summary>
        /// Monitor changes in the collection, numeric changes, add, remove, etc <br />
        /// Not internal items changes
        /// </summary>
        public  event Action<T?, NotifyCollectionChangedAction>? OnChanged
        {
            add { if(!IsEventHandlerRegistered(value)) _onChanged += value; }
            remove { _onChanged -= value; }
        }

        protected void Changed(T? card, NotifyCollectionChangedAction action)
        {
            _onChanged?.Invoke(card, action);
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

        #endregion
        #region BASIC LIST METHODS

        public virtual bool Contains(T item)
        {
            lock (_lock ?? new object())
                return _items.ContainsValue(item);
        }

        public virtual void Add(T item)
        {
            bool updated = false;
            lock (_lock ?? new object())
            {
                foreach (var key in item.Keys)
                {
                    _items.Add(key, item);
                    updated = true;                    
                }
            }    
            
            if(updated)
            {
                // Trigering collection changed
                Changed(item, NotifyCollectionChangedAction.Add);
            }
        }

        public virtual bool Remove(T item)
        {
            bool updated = false;
            lock (_lock ?? new object())
            {
                foreach (var key in item.Keys)
                {
                    if (_items.ContainsKey(key))
                    {
                        _items.Remove(key);
                        updated = true;
                    }
                }
            }

            if (updated)
            {
                // Trigering collection changed
                Changed(item, NotifyCollectionChangedAction.Remove);
            }

            return updated;
        }

        public virtual IList<T> ToList()
        {
            lock (_lock ?? new object())            
                return _items.Values.OfType<T>().ToList();            
        }

        public virtual IList<CustomT> ToList<CustomT>()
        {
            lock (_lock ?? new object())
            {
                return _items.Values.OfType<CustomT>().ToList();
            }
        }

        public virtual void Clear()
        {
            lock (_lock ?? new object())
            {
                _items.Clear();

                // Trigering collection changed
                Changed(default, NotifyCollectionChangedAction.Reset);                
            }
        }

        public int Count => _items.Values.Count;

        public bool IsReadOnly => false;

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_lock ?? new object())
            {
                _items.CopyTo(array, arrayIndex);                
            }
        }


        #endregion
        #region IMPLEMENT INTERFACE ENUMERATOR

        public IEnumerator<T> GetEnumerator()
        {
            lock (_lock ?? new object())
                return _items.Values.OfType<T>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }
}
