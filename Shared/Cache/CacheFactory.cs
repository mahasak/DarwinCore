namespace Darwin.Shared.Cache 
{
    using System;
    public static class CacheFactory
    {
        public static ICacheManager<TCacheValue> Build<TCacheValue>(string cacheName, ICache<string, TCacheValue> cache, ExpirationMode mode, TimeSpan timeout) 
        {
            return null;
        }
    }
}