using System;

namespace Darwin.Shared.Cache 
{
    public interface ICache<TCacheValue> : IDisposable
    {
 
        TCacheValue this[string key] { get; set; }

        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1023:IndexersShouldNotBeMultidimensional", Justification = "nope")]
        TCacheValue this[string key, string region] { get; set; }

        bool Add(string key, TCacheValue value);

        bool Add(string key, TCacheValue value, string region);

        bool Add(CacheItem<TCacheValue> item);

        void Clear();

        void ClearRegion(string region);

        bool Exists(string key);

        bool Exists(string key, string region);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get", Justification = "Maybe at some point.")]
        TCacheValue Get(string key);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get", Justification = "Maybe at some point.")]
        TCacheValue Get(string key, string region);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get", Justification = "Maybe at some point.")]
        TOut Get<TOut>(string key);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get", Justification = "Maybe at some point.")]
        TOut Get<TOut>(string key, string region);

        CacheItem<TCacheValue> GetCacheItem(string key);

        CacheItem<TCacheValue> GetCacheItem(string key, string region);

        void Put(string key, TCacheValue value);

        void Put(string key, TCacheValue value, string region);

        void Put(CacheItem<TCacheValue> item);

        bool Remove(string key);

        bool Remove(string key, string region);
    }
}