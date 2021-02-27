using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Timers;
using Darwin.Shared.Cache.Model;

namespace Darwin.Shared.Cache
{
    public class CacheCrow<K, V> : ICache<K, V>
    {
        public event EmptyCacheHandler EmptyCacheEvent;
        public int DormantCacheCount => _secondaryCache.Count;
        private readonly int _cacheSize;
        private readonly int _activeCacheExpire;
        private static ConcurrentDictionary<K, CacheItem<V>> _cacheDic;
        private static ConcurrentDictionary<K, Timer> _timerDic;
        private static CacheCrow<K, V> _cache;
        private static Timer _cleaner;
        private readonly IRemoteCache<K, V> _secondaryCache;
        public static ICache<K, V> Initialize(int size = 1000, int activeCacheExpire = 300000, int cleanerSnoozeTime = 400000)
        {
            if (_cache == null)
            {
                _cache = new CacheCrow<K, V>(size, activeCacheExpire, cleanerSnoozeTime);
            }
            _cache.LoadCache();
            return _cache;
        }
        
        public static ICache<K, V> Initialize(IRemoteCache<K, V> secondaryCache, int size = 1000, int activeCacheExpire = 300000, int cleanerSnoozeTime = 400000)
        {
            if (_cache == null)
            {
                _cache = new CacheCrow<K, V>(size, activeCacheExpire, cleanerSnoozeTime, secondaryCache);
            }
            _cache.LoadCache();
            return _cache;
        }

        public static ICache<K, V> GetCacheCrow => _cache;

        public int ActiveCount => _cacheDic.Count;

        public int Count => ActiveCount + ReadBinary().Count;

        public int PreviousCount => ActiveCount + DormantCacheCount;
        
        public void Clear()
        {
            _cacheDic.Clear();
            _timerDic.Clear();
            _cleaner.Stop();
            _cleaner.Start();
            WriteCache();
            EmptyCacheEvent?.Invoke(this, new EventArgs());
        }

        public void Add(K key, V data)
        {
            Add(key, data, null);
        }

        public void Add(K key, V data, Func<V> UpdateOnExpire)
        {
            _secondaryCache.EnsureExists();
            if (data != null)
            {
                Add(key, data, 1, UpdateOnExpire);
            }
        }

        public bool Update(K key, V data)
        {
            _secondaryCache.EnsureExists();
            if (data != null)
            {
                if (_cacheDic.ContainsKey(key))
                {
                    var CacheItem = _cacheDic[key];
                    CacheItem.ModifyDate = DateTime.Now;
                    CacheItem.Data = data;
                    _cacheDic[key] = CacheItem;
                    _timerDic[key].Stop();
                    _timerDic[key].Start();
                    return true;
                }
                else
                {
                    var dic = ReadBinary();
                    if (dic.ContainsKey(key))
                    {
                        var CacheItem = _cacheDic[key];
                        CacheItem.ModifyDate = DateTime.Now;
                        CacheItem.Data = data;
                        _cacheDic[key] = CacheItem;
                        _timerDic[key].Stop();
                        _timerDic[key].Start();
                        return true;
                    }
                }
            }
            return false;
        }

        public bool ActiveLookUp(K key)
        {
            if (_cacheDic.ContainsKey(key))
            {
                var CacheItem = _cacheDic[key];
                CacheItem.Frequency += 1;
                _cacheDic[key] = CacheItem;
                return true;
            }
            return false;
        }

        public bool LookUp(K key)
        {
            if (_cacheDic.ContainsKey(key))
            {
                var CacheItem = _cacheDic[key];
                CacheItem.Frequency += 1;
                _cacheDic[key] = CacheItem;
                return true;
            }
            return DeepLookUp(key);
        }

        public CacheItem<V> ActiveRemove(K key)
        {
            CacheItem<V> i = new CacheItem<V>();
            if (_cacheDic.ContainsKey(key) && (_timerDic.ContainsKey(key)))
            {
                var cacheTimer = _timerDic[key];
                _cacheDic.TryRemove(key, out i);
                cacheTimer.Elapsed -= new ElapsedEventHandler(Elapsed_Event);
                cacheTimer.Enabled = false;
                cacheTimer.AutoReset = false;
                cacheTimer.Stop();
                cacheTimer.Close();
                cacheTimer.Dispose();
                if (DormantCacheCount == 0 && ActiveCount == 0)
                {
                    EmptyCacheEvent?.Invoke(this, new EventArgs());
                }
            }
            return i;
        }

        public CacheItem<V> Remove(K key)
        {
            CacheItem<V> i = new CacheItem<V>();
            if (_cacheDic.ContainsKey(key) && (_timerDic.ContainsKey(key)))
            {
                var cacheTimer = _timerDic[key];
                _cacheDic.TryRemove(key, out i);
                cacheTimer.Elapsed -= new ElapsedEventHandler(Elapsed_Event);
                cacheTimer.Enabled = false;
                cacheTimer.AutoReset = false;
                cacheTimer.Stop();
                cacheTimer.Close();
                cacheTimer.Dispose();
                if (DormantCacheCount == 0 && ActiveCount == 0)
                {
                    EmptyCacheEvent?.Invoke(this, new EventArgs());
                }
            }
            else
            {
                var dic = ReadBinary();
                if (dic.ContainsKey(key))
                {
                    if (dic.TryRemove(key, out i))
                    {
                        WriteBinary(dic);
                    }
                }
            }
            return i;
        }

        public V GetValue(K key)
        {
            ConcurrentDictionary<K, CacheItem<V>> dic;
            if (ActiveLookUp(key))
            {
                return _cacheDic[key].Data;
            }
            else if ((dic = ReadBinary()) != null && dic.ContainsKey(key))
            {
                var CacheItem = dic[key];
                CacheItem.Frequency += 1;
                dic[key] = CacheItem;
                WriteBinary(dic);
                PerformLFUAndReplace(key, CacheItem);
                return CacheItem.Data;
            }
            else
                return default;
        }

        public V GetActiveValue(K key)
        {
            if (ActiveLookUp(key))
            {
                return _cacheDic[key].Data;
            }
            return default;
        }

        public void Dispose()
        {
            var dic = ReadBinary();
            if (dic.Count > _cacheDic.Count)
            {
                foreach (var t in _cacheDic)
                {
                    if (dic.ContainsKey(t.Key))
                    {
                        dic[t.Key] = t.Value;
                    }
                    else
                    {
                        dic.TryAdd(t.Key, t.Value);
                    }
                }
                WriteBinary(dic);
            }
            else
            {
                foreach (var t in dic)
                {
                    _cacheDic.TryAdd(t.Key, t.Value);
                }
                WriteBinary(_cacheDic);
            }
            foreach (var cacheTimer in _timerDic)
            {
                cacheTimer.Value.Elapsed -= new ElapsedEventHandler(Elapsed_Event);
                cacheTimer.Value.Enabled = false;
                cacheTimer.Value.AutoReset = false;
                cacheTimer.Value.Stop();
                cacheTimer.Value.Close();
                cacheTimer.Value.Dispose();
            }
            _cacheDic = null;
            _timerDic = null;
        }
        static CacheCrow()
        {
            _cacheDic = new ConcurrentDictionary<K, CacheItem<V>>();
            _timerDic = new ConcurrentDictionary<K, Timer>();
        }

        protected void LoadCache()
        {
            var dic = ReadBinary();
            var orderderdic = dic.OrderByDescending(x => x.Value.Frequency).ToList();
            for (int i = 0; i < _cacheSize && i < orderderdic.Count; i++)
            {
                Add(orderderdic[i].Key, orderderdic[i].Value);
            }
            _cleaner.Start();
        }
        private CacheCrow(int size = 1000, int activeCacheExpire = 300000, int cleanerSnoozeTime = 120000, IRemoteCache<K, V> secondaryCache = null) : base()
        {
            if (secondaryCache != null)
            {
                _secondaryCache = secondaryCache;
            } else {
                _secondaryCache = null;
            }
            
            _cacheSize = size;
            _activeCacheExpire = activeCacheExpire;
            _cleaner = new Timer(cleanerSnoozeTime);
            _cleaner.Elapsed += new ElapsedEventHandler(Cleaner_Event);
        }

        private void WriteBinary(K item, CacheItem<V> value)
        {
            if (value == null)
            {
                return;
            }
            var dic = ReadBinary();
            dic.TryRemove(item, out _);
            dic.TryAdd(item, value);
            WriteBinary(dic);
        }
        private void WriteBinary(ConcurrentDictionary<K, CacheItem<V>> dic)
        {
            _secondaryCache.WriteCache(dic);
        }
        private void WriteCache()
        {
            _secondaryCache.WriteCache(_cacheDic);
        }
        private ConcurrentDictionary<K, CacheItem<V>> ReadBinary()
        {
            _secondaryCache.EnsureExists();
            return _secondaryCache.ReadCache();
        }
        private void Add(K item, CacheItem<V> CacheItem, bool force = true)
        {
            if (CacheItem == null)
            {
                return;
            }
            Add(item, CacheItem.Data, CacheItem.Frequency, CacheItem.OnExpire, force);
        }
        private void Add(K item, V data, int frequency, Func<V> updateOnExpire = null, bool force = true)
        {
            if (item != null && !string.IsNullOrWhiteSpace(item.ToString()))
            {
                var CacheItem = new CacheItem<V>(item.ToString(), data)
                {
                    OnExpire = updateOnExpire
                };
                if (ActiveCount < _cacheSize)
                {
                    if (_cacheDic.TryAdd(item, CacheItem))
                    {
                        _cacheDic[item] = CacheItem;
                        _timerDic.TryRemove(item, out _);
                        var timer = new CacheTimer<K>(item, _activeCacheExpire);
                        timer.Elapsed += new ElapsedEventHandler(Elapsed_Event);
                        timer.Start();
                        _timerDic.TryAdd(item, timer);
                        var dic = ReadBinary();
                        dic.TryRemove(item, out CacheItem);
                        WriteBinary(dic);
                        return;
                    }
                }
                else
                {
                    if (!force)
                    {
                        return;
                    }
                    if (PerformLFUAndReplace(item, CacheItem))
                    {
                    }
                    else
                    {
                        WriteBinary(item, CacheItem);
                    }
                }
            }
        }

        protected int PerformLFUAndAdd()
        {

            CacheItem<V> i = new CacheItem<V>
            {
                Frequency = -1
            };
            var dic = new ConcurrentDictionary<K, CacheItem<V>>(ReadBinary());
            if (dic.Count < 1)
                return -1;
            var pairlist = dic.OrderByDescending(x => x.Value.Frequency).ToList();
            for (int j = 0; j < pairlist.Count && ActiveCount <= _cacheSize; j++)
            {
                if (pairlist[j].Key != null && pairlist[j].Value != null)
                    Add(pairlist[j].Key, pairlist[j].Value, false);
            }
            return i.Frequency;
        }

        [Obsolete]
        protected int PerformLFUAndReplace_Deprecated(K key, CacheItem<V> value)
        {
            CacheItem<V> i = new CacheItem<V>
            {
                Frequency = -1
            };
            if (_cacheSize > _cacheDic.Count)
            {
                var dic = ReadBinary();
                var pair = dic.OrderByDescending(x => x.Value.Frequency).FirstOrDefault();
                if (dic.Any() && pair.Key != null && pair.Key.Equals(key) || value == null)
                {
                    dic.TryRemove(pair.Key, out i);
                    WriteBinary(dic);
                    return PerformLFUAndReplace_Deprecated(key, value);
                }

                if (dic.Any() && pair.Key != null && pair.Value.Frequency > value.Frequency)
                {
                    Add(pair.Key, pair.Value);
                    dic.TryRemove(pair.Key, out i);
                    WriteBinary(dic);
                    return PerformLFUAndReplace_Deprecated(key, value);
                }
                else
                {
                    Add(key, value);
                    if (dic.Any() && pair.Key != null)
                    {
                        PerformLFUAndReplace_Deprecated(pair.Key, pair.Value);
                    }
                    else
                        return -1;
                }
            }
            else
            {
                var pair = _cacheDic.OrderBy(x => x.Value.Frequency).FirstOrDefault();
                if (_cacheDic.Any() && pair.Value.Frequency < value.Frequency || pair.Value.Frequency < 2)
                {
                    i = ActiveRemove(pair.Key);
                    if (i.Frequency > -1) // has been removed condition, add to new key to cache and add removed key to disk.
                    {
                        WriteBinary(pair.Key, pair.Value);
                        Add(key, value);
                    }
                }
                else
                {
                    if (key != null && !string.IsNullOrWhiteSpace(key.ToString()))
                    {
                        WriteBinary(key, value);
                    }
                }
            }
            return i.Frequency;
        }


        protected bool PerformLFUAndReplace(K key, CacheItem<V> value)
        {
            CacheItem<V> tmp = null;
            bool updated = false;
            int emptySlots = _cacheSize - _cacheDic.Count;
            try
            {
                if (emptySlots > 0)
                {
                    var dic = ReadBinary();

                    var orderedCacheItem = dic.Where(x => x.Value.Frequency > value.Frequency).Take(emptySlots);
                    foreach (var CacheItem in orderedCacheItem)
                    {
                        dic.TryRemove(CacheItem.Key, out tmp);
                        Add(CacheItem.Key, CacheItem.Value.Data);
                        updated = true;
                    }

                    if (updated)
                    {
                        WriteBinary(dic);
                    }
                    else
                    {
                        Add(key, value);
                        updated = true;
                    }
                }
                else
                {
                    var dic = _cacheDic;
                    var leastFrequencyCacheItem = dic.OrderBy(x => x.Value.Frequency).FirstOrDefault();
                    if (leastFrequencyCacheItem.Value.Frequency >= value.Frequency)
                    {
                        WriteBinary(key, value);
                        updated = true;
                    }
                    else
                    {
                        if (_cacheDic.TryRemove(leastFrequencyCacheItem.Key, out tmp))
                        {
                            WriteBinary(leastFrequencyCacheItem.Key, tmp);
                            _cacheDic.TryAdd(key, value);
                            updated = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return updated;
        }

        private bool DeepLookUp(K item)
        {
            Console.WriteLine("Performing Deep Lookup..");
            var dic = ReadBinary();
            if (dic.ContainsKey(item))
            {
                var CacheItem = dic[item];
                CacheItem.Frequency += 1;
                //CacheItem.CreateDate = DateTime.Now;
                dic[item] = CacheItem;
                WriteBinary(dic);
                PerformLFUAndReplace(item, CacheItem);
                return true;
            }

            return false;
        }
        private void Cleaner_Event(object sender, ElapsedEventArgs e)
        {
            System.Threading.Thread cleaner = new System.Threading.Thread(() =>
            {
                var dic = ReadBinary();
                WriteBinary(dic);
                if (DormantCacheCount == 0 && ActiveCount == 0)
                {
                    EmptyCacheEvent?.Invoke(this, new EventArgs());
                }
            });
            cleaner.Start();
        }
        private void Elapsed_Event(object sender, ElapsedEventArgs e)
        {
            if (_cacheDic == null)
            {
                return;
            }
            var cacheTimer = (CacheTimer<K>)sender;
            var CacheItem = _cacheDic.ContainsKey(cacheTimer.Key) ? _cacheDic[cacheTimer.Key] : null;
            if (CacheItem != null && CacheItem.OnExpire != null)
            {
                V newData = CacheItem.OnExpire.Invoke();
                Update(cacheTimer.Key, newData);
            }
            else
            {
                System.Threading.Thread cacheCleaner = new System.Threading.Thread(() =>
                {
                    var cachetimer = (CacheTimer<K>)sender;
                    cachetimer.Elapsed -= new ElapsedEventHandler(Elapsed_Event);
                    cachetimer.Close();
                    _cacheDic.TryRemove(cachetimer.Key, out _);
                    if (DormantCacheCount == 0 && ActiveCount == 0 && EmptyCacheEvent != null)
                    {
                        EmptyCacheEvent(this, new EventArgs());
                    }
                    else if (DormantCacheCount != 0)
                    {
                        var pair = ReadBinary().OrderByDescending(x => x.Value.Frequency).FirstOrDefault();
                        PerformLFUAndReplace(pair.Key, pair.Value);
                    }
                });
                cacheCleaner.Start();
            }
        }
    }
}