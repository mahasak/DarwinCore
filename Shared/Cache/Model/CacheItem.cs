using System;
using Darwin.Shared.Cache;
using static Darwin.Shared.Utility.Guard;
namespace Darwin.Shared.Cache.Model
{
    [Serializable]
    public class CacheItem<V>
    {
        public string Key { get; }
        public int Frequency { get; set;} = 1;
        public V Data { get; set;}
        public string Region { get; }
        public ExpirationMode ExpirationMode { get; }
        public DateTime CreateDate { get; }

        public DateTime ModifyDate { get; set;}

        public DateTime LastAccessDate { get; set;}
        public TimeSpan ExpirationTimeout { get; }
        public Func<V> OnExpire { get; set; }
        public bool ExpireByDefault { get; } = true;
        public Type DataType { get; }
        public bool IsExpired
        {
            get
            {
                var now = DateTime.UtcNow;
                return ((ExpirationMode == ExpirationMode.Absolute) && CreateDate.Add(ExpirationTimeout) < now) ||
                    ((ExpirationMode == ExpirationMode.Sliding) && LastAccessDate.Add(ExpirationTimeout) < now);
            }
        }

        public override string ToString()
        {
            return !string.IsNullOrWhiteSpace(Region) ?
                $"'{Region}:{Key}', exp:{ExpirationMode.ToString()} {ExpirationTimeout}, lastAccess:{LastAccessDate}"
                : $"'{Key}', exp:{ExpirationMode.ToString()} {ExpirationTimeout}, lastAccess:{LastAccessDate}";
        }

        public CacheItem(string key, V data)
            : this(key, data, null, null, null, null, null)
        {
            Data = data;
            CreateDate = DateTime.Now;
        }
        public CacheItem(string key, V data, string region)
            : this(key, data, region, null, null, null, null)
        {
            Data = data;
            CreateDate = DateTime.Now;
        }

        public CacheItem(string key, V data, ExpirationMode mode, TimeSpan timeout)
            : this(key, data, null, mode, timeout, null, null)
        {
            Data = data;
            CreateDate = DateTime.Now;
        }

        public CacheItem(string key, V data, string region, ExpirationMode mode, TimeSpan timeout)
            : this(key, data, region, mode, timeout, null, null)
        {
            Data = data;
            CreateDate = DateTime.Now;
        }

        public CacheItem()
        {
        }

        private CacheItem(string key, V data, string region, ExpirationMode? mode, TimeSpan? timeout, DateTime? created, DateTime? lastAccess, bool expirationByDefault = true)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNull(data, nameof(data));

            Key = key;
            Region = region;
            Data = data;
            DataType = data.GetType();
            ExpirationMode = mode ?? ExpirationMode.None;
            ExpirationTimeout = ExpirationMode == ExpirationMode.None ? TimeSpan.Zero : timeout ?? TimeSpan.Zero;
            ExpireByDefault = expirationByDefault;

            if (ExpirationTimeout.TotalDays > 365)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), "Expiration timeout must be between 00:00:00 and 365:00:00:00.");
            }

            if (ExpirationMode != ExpirationMode.None && ExpirationTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), "Expiration timeout must be greater than zero if expiration mode is defined.");
            }

            if (created.HasValue && created.Value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException($"Created date kind must be {DateTimeKind.Utc}.", nameof(created));
            }

            if (lastAccess.HasValue && lastAccess.Value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException($"Last accessed date kind must be {DateTimeKind.Utc}.", nameof(lastAccess));
            }

            CreateDate = created ?? DateTime.UtcNow;
            LastAccessDate = lastAccess ?? DateTime.UtcNow;
        }
    }
}