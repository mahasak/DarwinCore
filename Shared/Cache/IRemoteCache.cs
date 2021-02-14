using System.Collections.Concurrent;
using Darwin.Shared.Cache.Model;

namespace Darwin.Shared.Cache
{
    public interface IRemoteCache<K, V>
    {
        int Count { get; }
        double CacheExpireInMilliseconds { get; }
        ConcurrentDictionary<K, CacheItem<V>> ReadCache();
        void WriteCache(ConcurrentDictionary<K, CacheItem<V>> cache);
        void Clear();
        bool Exists();
        bool IsAccessible();
        void EnsureExists();
        CacheItem<V> LookUp(K key);
        bool IsEmpty();
    }
}