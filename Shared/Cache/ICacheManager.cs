namespace Darwin.Shared.Cache 
{
    using System;
    using Darwin.Shared.Cache.Model;
    public interface ICacheManager<TCacheType> {
        void Clear();
        void Add(string key, TCacheType data);
        void Add(string Key, TCacheType data, Func<TCacheType> UpdateOnExpire);
        bool Update(string key, TCacheType data);
        bool LookUp(string key, bool localOnly = false);
        CacheItem<TCacheType> Remove(string key, bool localOnly = false);
        TCacheType Get(string key, bool localOnly = false);
        event EmptyCacheHandler EmptyCacheEvent;
    }
}