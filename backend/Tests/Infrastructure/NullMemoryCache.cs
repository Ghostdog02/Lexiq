using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// No-op IMemoryCache for tests. Always reports a cache miss so every service call
/// fetches fresh data from the DB — prevents warm-up or cross-call stale data.
/// </summary>
public sealed class NullMemoryCache : IMemoryCache
{
    public bool TryGetValue(object key, out object? value)
    {
        value = null;
        return false;
    }

    public ICacheEntry CreateEntry(object key) => new NullCacheEntry(key);

    public void Remove(object key) { }

    public void Dispose() { }
}

file sealed class NullCacheEntry(object key) : ICacheEntry
{
    public object Key { get; } = key;
    public object? Value { get; set; }
    public DateTimeOffset? AbsoluteExpiration { get; set; }
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }
    public IList<IChangeToken> ExpirationTokens { get; } = [];
    public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; } = [];
    public CacheItemPriority Priority { get; set; }
    public long? Size { get; set; }

    public void Dispose() { }
}
