using System.Collections.Concurrent;
using Bellows.Abstractions;

namespace Bellows;

/// <summary>
/// Default implementation of IMediator
/// </summary>
public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MediatorOptions _options;
    private static readonly ConcurrentDictionary<Type, Type> _requestHandlerTypes = new();
    private static readonly ConcurrentDictionary<Type, Type[]> _notificationHandlerTypes = new();

    public Mediator(IServiceProvider serviceProvider, MediatorOptions? options = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options ?? new MediatorOptions();
    }

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var requestType = request.GetType();
        var stopwatch = _options.EnablePerformanceMonitoring ? System.Diagnostics.Stopwatch.StartNew() : null;

        try
        {
            // Get pipeline behaviors for the concrete request type
            var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
            var behaviors = GetServices(behaviorType).Reverse().ToList();

            // Build the handler delegate
            RequestHandlerDelegate<TResponse> handler = () => InvokeHandler(request, cancellationToken);

            // Wrap handler with behaviors (in reverse order so first registered runs first)
            foreach (var behavior in behaviors)
            {
                var currentBehavior = behavior;
                var currentHandler = handler;
                handler = () =>
                {
                    // Use dynamic to call the Handle method with the correct types
                    var handleMethod = currentBehavior.GetType().GetMethod("Handle");
                    if (handleMethod == null)
                        throw new InvalidOperationException($"Handle method not found on behavior {currentBehavior.GetType().Name}");

                    return (Task<TResponse>)handleMethod.Invoke(currentBehavior, new object[] { request, currentHandler, cancellationToken })!;
                };
            }

            // Apply timeout if configured
            TResponse result;
            if (_options.RequestTimeout.HasValue)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_options.RequestTimeout.Value);
                result = await handler();
            }
            else
            {
                result = await handler();
            }

            return result;
        }
        finally
        {
            // Performance monitoring
            if (stopwatch != null)
            {
                stopwatch.Stop();
                if (stopwatch.ElapsedMilliseconds > _options.PerformanceThresholdMs)
                {
                    _options.OnPerformanceThresholdExceeded?.Invoke(requestType.Name, stopwatch.ElapsedMilliseconds);
                }
            }
        }
    }

    private async Task<TResponse> InvokeHandler<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        var requestType = request.GetType();
        var handlerType = _requestHandlerTypes.GetOrAdd(requestType, static rt =>
        {
            var responseType = rt.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
                .Select(i => i.GetGenericArguments()[0])
                .FirstOrDefault();

            if (responseType == null)
                throw new InvalidOperationException($"Could not determine response type for {rt.Name}");

            return typeof(IRequestHandler<,>).MakeGenericType(rt, responseType);
        });

        var handler = _serviceProvider.GetService(handlerType);
        if (handler == null)
        {
            if (_options.ThrowOnMissingHandler)
                throw new InvalidOperationException($"No handler registered for {requestType.Name}");

            return default(TResponse)!;
        }

        var handleMethod = handlerType.GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.Handle));
        if (handleMethod == null)
            throw new InvalidOperationException($"Handle method not found on handler for {requestType.Name}");

        var result = await ((Task<TResponse>)handleMethod.Invoke(handler, new object[] { request, cancellationToken })!);
        return result;
    }

    public async Task Publish(INotification notification, CancellationToken cancellationToken = default)
    {
        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        var notificationType = notification.GetType();
        var handlerTypes = _notificationHandlerTypes.GetOrAdd(notificationType, static nt =>
        {
            var handlerType = typeof(INotificationHandler<>).MakeGenericType(nt);
            return new[] { handlerType };
        });

        var exceptionContext = new ExceptionContext();

        if (_options.NotificationPublishStrategy == NotificationPublishStrategy.Sequential)
        {
            // Execute handlers sequentially
            await ExecuteSequential(handlerTypes, notification, cancellationToken, exceptionContext);
        }
        else
        {
            // Execute handlers in parallel
            await ExecuteParallel(handlerTypes, notification, cancellationToken, exceptionContext);
        }

        // Handle exceptions based on strategy
        HandleExceptions(exceptionContext);
    }

    private async Task ExecuteSequential(
        Type[] handlerTypes,
        INotification notification,
        CancellationToken cancellationToken,
        ExceptionContext exceptionContext)
    {
        foreach (var handlerType in handlerTypes)
        {
            var handlers = GetServices(handlerType);
            foreach (var handler in handlers)
            {
                try
                {
                    var handleMethod = handlerType.GetMethod(nameof(INotificationHandler<INotification>.Handle));
                    if (handleMethod != null)
                    {
                        var task = (Task)handleMethod.Invoke(handler, new object[] { notification, cancellationToken })!;
                        await task;
                    }
                }
                catch (Exception ex)
                {
                    var actualException = ex is System.Reflection.TargetInvocationException tie ? tie.InnerException ?? ex : ex;
                    exceptionContext.AddException(actualException);

                    if (_options.NotificationExceptionHandling == NotificationExceptionHandlingStrategy.StopOnFirstException)
                    {
                        throw actualException;
                    }
                }
            }
        }
    }

    private async Task ExecuteParallel(
        Type[] handlerTypes,
        INotification notification,
        CancellationToken cancellationToken,
        ExceptionContext exceptionContext)
    {
        var tasks = new List<Task>();
        var semaphore = _options.MaxConcurrentNotifications.HasValue
            ? new SemaphoreSlim(_options.MaxConcurrentNotifications.Value)
            : null;

        try
        {
            foreach (var handlerType in handlerTypes)
            {
                var handlers = GetServices(handlerType);
                foreach (var handler in handlers)
                {
                    var handleMethod = handlerType.GetMethod(nameof(INotificationHandler<INotification>.Handle));
                    if (handleMethod != null)
                    {
                        var task = ExecuteHandlerWithSemaphore(handler, handleMethod, notification, cancellationToken, semaphore, exceptionContext);
                        tasks.Add(task);
                    }
                }
            }

            await Task.WhenAll(tasks);
        }
        finally
        {
            semaphore?.Dispose();
        }
    }

    private async Task ExecuteHandlerWithSemaphore(
        object handler,
        System.Reflection.MethodInfo handleMethod,
        INotification notification,
        CancellationToken cancellationToken,
        SemaphoreSlim? semaphore,
        ExceptionContext exceptionContext)
    {
        if (semaphore != null)
        {
            await semaphore.WaitAsync(cancellationToken);
        }

        try
        {
            var task = (Task)handleMethod.Invoke(handler, new object[] { notification, cancellationToken })!;
            await task;
        }
        catch (Exception ex)
        {
            var actualException = ex is System.Reflection.TargetInvocationException tie ? tie.InnerException ?? ex : ex;
            exceptionContext.AddException(actualException);

            if (_options.NotificationExceptionHandling == NotificationExceptionHandlingStrategy.StopOnFirstException)
            {
                throw actualException;
            }
        }
        finally
        {
            semaphore?.Release();
        }
    }

    private void HandleExceptions(ExceptionContext exceptionContext)
    {
        if (!exceptionContext.HasExceptions)
            return;

        switch (_options.NotificationExceptionHandling)
        {
            case NotificationExceptionHandlingStrategy.StopOnFirstException:
                // Exception already thrown in execution methods
                break;

            case NotificationExceptionHandlingStrategy.ContinueOnException:
                if (exceptionContext.FirstException != null)
                    throw exceptionContext.FirstException;
                break;

            case NotificationExceptionHandlingStrategy.AggregateExceptions:
                throw new AggregateException("One or more notification handlers threw exceptions", exceptionContext.Exceptions);

            case NotificationExceptionHandlingStrategy.SuppressExceptions:
                // Do nothing, suppress all exceptions
                break;
        }
    }

    private class ExceptionContext
    {
        private readonly List<Exception> _exceptions = new();
        private readonly object _lock = new();
        private Exception? _firstException;

        public void AddException(Exception exception)
        {
            lock (_lock)
            {
                _firstException ??= exception;
                _exceptions.Add(exception);
            }
        }

        public bool HasExceptions => _exceptions.Count > 0;
        public Exception? FirstException => _firstException;
        public IReadOnlyList<Exception> Exceptions => _exceptions;
    }

    private IEnumerable<object> GetServices(Type serviceType)
    {
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(serviceType);
        var services = _serviceProvider.GetService(enumerableType);

        if (services == null)
            return Array.Empty<object>();

        return ((System.Collections.IEnumerable)services).Cast<object>();
    }
}
