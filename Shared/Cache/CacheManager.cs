namespace Darwin.Shared.Cache 
{
    using System;
    using System.Collections.Concurrent;
    using Darwin.Shared.Cache.Model;

    public class CacheManager<TCacheType>  : ICacheManager<TCacheType> 
    {
        private ICache<string, CacheItem<TCacheType>> _cache;
        private static ConcurrentDictionary<string, CacheItem<TCacheType>> _localCache;
        private ExpirationMode _mode;
        private TimeSpan _timeout;

        public event EmptyCacheHandler EmptyCacheEvent;

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void Add(string key, TCacheType data)
        {
            throw new NotImplementedException();
        }

        public void Add(string Key, TCacheType data, Func<TCacheType> UpdateOnExpire)
        {
            throw new NotImplementedException();
        }

        public bool Update(string key, TCacheType data)
        {
            throw new NotImplementedException();
        }

        public bool LookUp(string key, bool localOnly = false)
        {
            throw new NotImplementedException();
        }

        public CacheItem<TCacheType> Remove(string key, bool localOnly = false)
        {
            throw new NotImplementedException();
        }

        public TCacheType Get(string key, bool localOnly = false)
        {
            throw new NotImplementedException();
        }
    }
}