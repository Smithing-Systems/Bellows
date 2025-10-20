namespace Bellows.Abstractions;

/// <summary>
/// Configuration options for the mediator
/// </summary>
public class MediatorOptions
{
    /// <summary>
    /// Determines how notification handlers are executed.
    /// Default: Parallel
    /// </summary>
    public NotificationPublishStrategy NotificationPublishStrategy { get; set; } = NotificationPublishStrategy.Parallel;

    /// <summary>
    /// Determines how exceptions in notification handlers are handled.
    /// Default: ContinueOnException (all handlers run, first exception is thrown)
    /// </summary>
    public NotificationExceptionHandlingStrategy NotificationExceptionHandling { get; set; } = NotificationExceptionHandlingStrategy.ContinueOnException;

    /// <summary>
    /// Maximum time to wait for a request handler to complete.
    /// If null (default), no timeout is enforced.
    /// </summary>
    public TimeSpan? RequestTimeout { get; set; }

    /// <summary>
    /// Whether to throw an exception when no handler is found for a request.
    /// Default: true (throw InvalidOperationException)
    /// When false, returns default(TResponse) instead
    /// </summary>
    public bool ThrowOnMissingHandler { get; set; } = true;

    /// <summary>
    /// Whether to enable automatic performance monitoring.
    /// When enabled, logs warnings for requests exceeding PerformanceThreshold.
    /// Default: false
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = false;

    /// <summary>
    /// Performance threshold in milliseconds.
    /// Requests taking longer than this will trigger a warning (if EnablePerformanceMonitoring is true).
    /// Default: 1000ms (1 second)
    /// </summary>
    public int PerformanceThresholdMs { get; set; } = 1000;

    /// <summary>
    /// Action to invoke when a request exceeds the performance threshold.
    /// Receives request type name and elapsed milliseconds.
    /// </summary>
    public Action<string, long>? OnPerformanceThresholdExceeded { get; set; }

    /// <summary>
    /// Maximum number of notification handlers that can execute concurrently in parallel mode.
    /// If null (default), no limit is enforced.
    /// Only applies when NotificationPublishStrategy is Parallel.
    /// </summary>
    public int? MaxConcurrentNotifications { get; set; }
}

/// <summary>
/// Strategy for publishing notifications to handlers
/// </summary>
public enum NotificationPublishStrategy
{
    /// <summary>
    /// Execute all handlers in parallel using Task.WhenAll
    /// </summary>
    Parallel,

    /// <summary>
    /// Execute handlers sequentially in registration order
    /// </summary>
    Sequential
}

/// <summary>
/// Strategy for handling exceptions in notification handlers
/// </summary>
public enum NotificationExceptionHandlingStrategy
{
    /// <summary>
    /// Stop on the first exception and throw it immediately.
    /// Remaining handlers will not execute.
    /// </summary>
    StopOnFirstException,

    /// <summary>
    /// Continue executing all handlers even if some throw exceptions.
    /// The first exception encountered will be thrown after all handlers complete.
    /// </summary>
    ContinueOnException,

    /// <summary>
    /// Continue executing all handlers and aggregate all exceptions.
    /// Throws an AggregateException containing all exceptions after all handlers complete.
    /// </summary>
    AggregateExceptions,

    /// <summary>
    /// Suppress all exceptions from notification handlers.
    /// No exceptions will be thrown, but handlers will still execute.
    /// </summary>
    SuppressExceptions
}
