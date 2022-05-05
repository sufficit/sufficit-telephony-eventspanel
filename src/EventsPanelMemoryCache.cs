using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Caching;
using System.Collections;
using Sufficit.Asterisk.Manager.Events;

namespace Sufficit.Telephony.EventsPanel
{
    public class EventsPanelMemoryCache<T> : IEnumerable<T> where T : IManagerEvent
    {
        protected readonly MemoryCache cache;
        protected TimeSpan defaultExpiration;

        public EventsPanelMemoryCache(TimeSpan expiration = default)
        {
            //not region name, aparently region is not supported
            var cacheName = Guid.NewGuid().ToString();
            cache = new MemoryCache(cacheName, null);

            if (expiration != default(TimeSpan))
                defaultExpiration = expiration;
            else 
                defaultExpiration = TimeSpan.FromMinutes(20);
        }

        ~EventsPanelMemoryCache()
        {
            foreach (var item in cache)
            {
                cache.Remove(item.Key);
            }
        }

        #region INTERFACE IENUMERABLE

        public IEnumerator<T> GetEnumerator() => OfType<T>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => OfType<T>().GetEnumerator();

        public virtual IEnumerable<TCustom> OfType<TCustom>()
        {            
            foreach (var item in cache)
            {
                if (item.Value != null && item.Value is TCustom value)
                    yield return value;
            }
        }

        #endregion

        public virtual bool Add(T obj)
        {
            var policy = new CacheItemPolicy() { SlidingExpiration = defaultExpiration };
            return cache.Add(Guid.NewGuid().ToString(), obj, policy);
        }
    }
}
