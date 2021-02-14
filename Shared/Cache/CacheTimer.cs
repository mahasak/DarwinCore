using System.Timers;

namespace Darwin.Shared.Cache
{
    public class CacheTimer<K> : Timer
    {
        public K Key { get; set; }
        public CacheTimer(K key, double interval = 60000) : base(interval)
        {
            Key = key;
        }
    }
}