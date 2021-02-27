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
    }
}