using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.AccessControl;
using System.Web;
using Darwin.Shared.Cache;
using System.Diagnostics;
using Darwin.Shared.Cache.Model;

namespace Darwin.Shared.Cache
{
    internal class DefaultSecondaryCache<K, V> : IRemoteCache<K, V>
    {
        private readonly string _cachePath;
        private readonly string _cacheDirectoryPath;

        public int Count { get; private set; }
        public double CacheExpireInMilliseconds { get; private set; }

        public DefaultSecondaryCache(double cacheExpireInMilliseconds)
        {
            CacheExpireInMilliseconds = cacheExpireInMilliseconds;
            Count = -1;

            string appDirectory;
            appDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            
            _cacheDirectoryPath = appDirectory + "_crow";
            _cachePath = _cacheDirectoryPath + @"\CacheCrow";
            if (!Directory.Exists(_cacheDirectoryPath))
            {
                CreateCacheDirectory();
            }
        }

        public void Clear()
        {
            if (IsAccessible())
            {
                File.Delete(_cachePath);
            }
        }

        public ConcurrentDictionary<K, CacheItem<V>> ReadCache()
        {
            ConcurrentDictionary<K, CacheItem<V>> dic = null;
            lock (this)
            {
                if (IsEmpty())
                {
                    using (FileStream fs = new FileStream(_cachePath, FileMode.Open))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        dic = (ConcurrentDictionary<K, CacheItem<V>>)bf.Deserialize(fs);
                        dic = GetValidCache(dic);
                    }
                }
                else
                {
                    dic = new ConcurrentDictionary<K, CacheItem<V>>();
                }
                Count = dic.Count;
            }
            return dic;
        }

        public void WriteCache(ConcurrentDictionary<K, CacheItem<V>> cache)
        {
            lock (this)
            {
                if (!Exists())
                {
                    CreateCacheDirectory();
                }
                using (FileStream fs = new FileStream(_cachePath, FileMode.Create))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(fs, cache);
                }
            }
        }

        private ConcurrentDictionary<K, CacheItem<V>> GetValidCache(ConcurrentDictionary<K, CacheItem<V>> cache)
        {
            return new ConcurrentDictionary<K, CacheItem<V>>(cache.Where(x => DateTime.Now.Subtract(x.Value.CreateDate).TotalMilliseconds < CacheExpireInMilliseconds));
        }

        private void CreateCacheDirectory()
        {
            Directory.CreateDirectory(_cacheDirectoryPath);
            DirectoryInfo dInfo = new DirectoryInfo(_cacheDirectoryPath);
            var ds = new DirectorySecurity(_cacheDirectoryPath, AccessControlSections.Access);
            dInfo.SetAccessControl(ds);
        }

        public bool Exists()
        {
            if (File.Exists(_cachePath))
            {
                return true;
            }
            return false;
        }

        public bool IsEmpty()
        {
            if (IsAccessible())
            {
                using (var fs = new FileStream(_cachePath, FileMode.Open))
                {
                    return fs.Length > 0;
                }
            }
            return true;
        }

        public bool IsAccessible()
        {
            if (!Exists())
            {
                return false;
            }

            var fileInfo = new FileInfo(_cachePath);
            try
            {
                using (var fileStream = fileInfo.Open(FileMode.Open, FileAccess.ReadWrite))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public void EnsureExists()
        {
            if (!Directory.Exists(_cacheDirectoryPath))
            {
                CreateCacheDirectory();
            }
        }

        public CacheItem<V> LookUp(K key)
        {
            throw new NotImplementedException();
        }
    }
}