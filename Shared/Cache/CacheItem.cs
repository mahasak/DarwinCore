using System;
using Darwin.Shared.Cache;

using static Darwin.Shared.Utility.Guard;

namespace CacheManager.Core
{
    public class CacheItem<T> : ICacheItemProperties
    {
        public CacheItem(string key, T value)
            : this(key, null, value, null, null, null)
        {
        }

        public CacheItem(string key, string region, T value)
            : this(key, region, value, null, null, null)
        {
            NotNullOrWhiteSpace(region, nameof(region));
        }

        public CacheItem(string key, T value, ExpirationMode expiration, TimeSpan timeout)
            : this(key, null, value, expiration, timeout, null, null, false)
        {
        }

        public CacheItem(string key, string region, T value, ExpirationMode expiration, TimeSpan timeout)
            : this(key, region, value, expiration, timeout, null, null, false)
        {
            NotNullOrWhiteSpace(region, nameof(region));
        }

        protected CacheItem()
        {
        }

        private CacheItem(string key, string region, T value, ExpirationMode? expiration, TimeSpan? timeout, DateTime? created, DateTime? lastAccessed = null, bool expirationDefaults = true)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNull(value, nameof(value));

            Key = key;
            Region = region;
            Value = value;
            ValueType = value.GetType();
            ExpirationMode = expiration ?? ExpirationMode.Default;
            ExpirationTimeout = (ExpirationMode == ExpirationMode.None || ExpirationMode == ExpirationMode.Default) ? TimeSpan.Zero : timeout ?? TimeSpan.Zero;
            UsesExpirationDefaults = expirationDefaults;

            if (ExpirationTimeout.TotalDays > 365)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), "Expiration timeout must be between 00:00:00 and 365:00:00:00.");
            }

            if (ExpirationMode != ExpirationMode.Default && ExpirationMode != ExpirationMode.None && ExpirationTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), "Expiration timeout must be greater than zero if expiration mode is defined.");
            }

            if (created.HasValue && created.Value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException($"Created date kind must be {DateTimeKind.Utc}.", nameof(created));
            }

            if (lastAccessed.HasValue && lastAccessed.Value.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException($"Last accessed date kind must be {DateTimeKind.Utc}.", nameof(lastAccessed));
            }

            CreateDate = created ?? DateTime.UtcNow;
            LastAccessDate = lastAccessed ?? DateTime.UtcNow;
        }

        public bool IsExpired
        {
            get
            {
                var now = DateTime.UtcNow;
                if (ExpirationMode == ExpirationMode.Absolute
                    && CreateDate.Add(ExpirationTimeout) < now)
                {
                    return true;
                }
                else if (ExpirationMode == ExpirationMode.Sliding
                    && LastAccessDate.Add(ExpirationTimeout) < now)
                {
                    return true;
                }

                return false;
            }
        }

        public DateTime CreateDate { get; }

        public ExpirationMode ExpirationMode { get; }

        public TimeSpan ExpirationTimeout { get; }

        public string Key { get; }

        public DateTime LastAccessDate { get; set; }

        public string Region { get; }

        public T Value { get; }

        public Type ValueType { get; }

        public bool UsesExpirationDefaults { get; } = true;


        public override string ToString()
        {
            return !string.IsNullOrWhiteSpace(Region) ?
                $"'{Region}:{Key}', exp:{ExpirationMode.ToString()} {ExpirationTimeout}, lastAccess:{LastAccessDate}"
                : $"'{Key}', exp:{ExpirationMode.ToString()} {ExpirationTimeout}, lastAccess:{LastAccessDate}";
        }

        internal CacheItem<T> WithExpiration(ExpirationMode mode, TimeSpan timeout, bool usesHandleDefault = true) =>
            new CacheItem<T>(Key, Region, Value, mode, timeout, mode == ExpirationMode.Absolute ? DateTime.UtcNow : CreateDate, LastAccessDate, usesHandleDefault);

        public CacheItem<T> WithAbsoluteExpiration(DateTimeOffset absoluteExpiration)
        {
            var timeout = absoluteExpiration - DateTimeOffset.UtcNow;
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("Expiration value must be greater than zero.", nameof(absoluteExpiration));
            }

            return WithExpiration(ExpirationMode.Absolute, timeout, false);
        }

        public CacheItem<T> WithAbsoluteExpiration(TimeSpan absoluteExpiration)
        {
            if (absoluteExpiration <= TimeSpan.Zero)
            {
                throw new ArgumentException("Expiration value must be greater than zero.", nameof(absoluteExpiration));
            }

            return WithExpiration(ExpirationMode.Absolute, absoluteExpiration, false);
        }

        public CacheItem<T> WithSlidingExpiration(TimeSpan slidingExpiration)
        {
            if (slidingExpiration <= TimeSpan.Zero)
            {
                throw new ArgumentException("Expiration value must be greater than zero.", nameof(slidingExpiration));
            }

            return WithExpiration(ExpirationMode.Sliding, slidingExpiration, false);
        }

        public CacheItem<T> WithNoExpiration() =>
            new CacheItem<T>(Key, Region, Value, ExpirationMode.None, TimeSpan.Zero, CreateDate, LastAccessDate, false);

        public CacheItem<T> WithDefaultExpiration() =>
            new CacheItem<T>(Key, Region, Value, ExpirationMode.Default, TimeSpan.Zero, CreateDate, LastAccessDate, true);

        public CacheItem<T> WithValue(T value) =>
            new CacheItem<T>(Key, Region, value, ExpirationMode, ExpirationTimeout, CreateDate, LastAccessDate, UsesExpirationDefaults);

        public CacheItem<T> WithCreated(DateTime created) =>
            new CacheItem<T>(Key, Region, Value, ExpirationMode, ExpirationTimeout, created, LastAccessDate, UsesExpirationDefaults);
    }
}