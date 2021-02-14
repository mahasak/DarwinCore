using System;
using Darwin.Shared.Cache.Model;

namespace Darwin.Shared.Cache
{
    public interface ICache<K, V> : IDisposable
    {
        int ActiveCount { get; }
        int Count { get; }
        int PreviousCount { get; }
        void Clear();
        void Add(K key, V data);
        void Add(K Key, V data, Func<V> UpdateOnExpire);
        bool Update(K key, V data);
        bool ActiveLookUp(K key);
        bool LookUp(K key);
        CacheItem<V> ActiveRemove(K key);
        CacheItem<V> Remove(K key);
        V GetValue(K key);
        V GetActiveValue(K key);
        event EmptyCacheHandler EmptyCacheEvent;
    }
    public delegate void EmptyCacheHandler(object sender, EventArgs args);
}