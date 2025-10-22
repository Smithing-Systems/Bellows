using System.Collections.Concurrent;
using Bellows.Abstractions;

namespace AspNetCoreSample.PipelineBehavior.PipelineBehaviors;

/// <summary>
/// Caches responses for requests that implement ICacheable
/// </summary>
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ConcurrentDictionary<string, CacheEntry> Cache = new();
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheable cacheable)
        {
            return await next();
        }

        var cacheKey = cacheable.GetCacheKey();
        var requestName = typeof(TRequest).Name;

        // Check cache
        if (Cache.TryGetValue(cacheKey, out var entry) && !entry.IsExpired)
        {
            _logger.LogInformation("CACHE HIT: {RequestName} with key {CacheKey}", requestName, cacheKey);
            return (TResponse)entry.Value;
        }

        _logger.LogInformation("CACHE MISS: {RequestName} with key {CacheKey}", requestName, cacheKey);

        // Execute request
        var response = await next();

        // Store in cache
        var expirationTime = DateTime.UtcNow.AddSeconds(cacheable.CacheDurationSeconds);
        Cache[cacheKey] = new CacheEntry(response!, expirationTime);

        _logger.LogInformation("CACHED: {RequestName} with key {CacheKey} (expires: {ExpirationTime})",
            requestName, cacheKey, expirationTime);

        return response;
    }

    private class CacheEntry
    {
        public object Value { get; }
        public DateTime ExpiresAt { get; }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;

        public CacheEntry(object value, DateTime expiresAt)
        {
            Value = value;
            ExpiresAt = expiresAt;
        }
    }
}

/// <summary>
/// Interface for requests that support caching
/// </summary>
public interface ICacheable
{
    string GetCacheKey();
    int CacheDurationSeconds { get; }
}
