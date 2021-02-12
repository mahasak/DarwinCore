using System;

namespace Darwin.Shared.Cache
{
    public interface ICacheItemProperties
    {
        DateTime CreateDate { get; }
        ExpirationMode ExpirationMode { get; }
        TimeSpan ExpirationTimeout { get; }
        string Key { get; }
        DateTime LastAccessDate { get; set; }
        string Region { get; }
        bool UsesExpirationDefaults { get; }
        Type ValueType { get; }
    }
}