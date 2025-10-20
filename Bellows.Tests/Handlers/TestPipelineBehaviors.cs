using Bellows.Abstractions;

namespace Bellows.Tests.Handlers;

// Logging behavior
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public static List<string> Logs { get; } = new();

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        Logs.Add($"[LoggingBehavior] Before: {requestName}");

        var response = await next();

        Logs.Add($"[LoggingBehavior] After: {requestName}");
        return response;
    }
}

// Validation behavior
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public static List<string> Logs { get; } = new();
    public static bool ShouldThrow { get; set; }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        Logs.Add("[ValidationBehavior] Validating");

        if (ShouldThrow)
        {
            throw new InvalidOperationException("Validation failed");
        }

        var response = await next();
        return response;
    }
}

// Performance monitoring behavior
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public static List<string> Logs { get; } = new();

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        Logs.Add($"[PerformanceBehavior] Start: {requestName}");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        Logs.Add($"[PerformanceBehavior] End: {requestName} - {stopwatch.ElapsedMilliseconds}ms");
        return response;
    }
}

// Caching behavior (simplified for testing)
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public static Dictionary<string, object> Cache { get; } = new();
    public static List<string> Logs { get; } = new();

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var key = $"{typeof(TRequest).Name}_{request}";

        if (Cache.TryGetValue(key, out var cached))
        {
            Logs.Add($"[CachingBehavior] Cache hit: {key}");
            return (TResponse)cached;
        }

        Logs.Add($"[CachingBehavior] Cache miss: {key}");
        var response = await next();
        Cache[key] = response!;
        return response;
    }
}

// Retry behavior
public class RetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public static List<string> Logs { get; } = new();
    public static int RetryCount { get; set; } = 3;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        for (int i = 0; i < RetryCount; i++)
        {
            try
            {
                Logs.Add($"[RetryBehavior] Attempt {i + 1}");
                return await next();
            }
            catch when (i < RetryCount - 1)
            {
                Logs.Add($"[RetryBehavior] Attempt {i + 1} failed, retrying...");
                await Task.Delay(10, cancellationToken);
            }
        }

        throw new InvalidOperationException("All retry attempts failed");
    }
}
   