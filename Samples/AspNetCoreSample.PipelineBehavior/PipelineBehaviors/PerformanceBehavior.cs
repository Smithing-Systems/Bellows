using System.Diagnostics;
using Bellows.Abstractions;

namespace AspNetCoreSample.PipelineBehavior.PipelineBehaviors;

/// <summary>
/// Monitors request execution time and logs warnings for slow requests
/// </summary>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private const int SlowRequestThresholdMs = 500;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();

        var requestName = typeof(TRequest).Name;
        var elapsedMs = stopwatch.ElapsedMilliseconds;

        if (elapsedMs > SlowRequestThresholdMs)
        {
            _logger.LogWarning("SLOW REQUEST: {RequestName} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
                requestName, elapsedMs, SlowRequestThresholdMs);
        }
        else
        {
            _logger.LogDebug("Performance: {RequestName} completed in {ElapsedMs}ms",
                requestName, elapsedMs);
        }

        return response;
    }
}
