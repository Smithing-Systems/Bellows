namespace Bellows.Abstractions;

/// <summary>
/// Mediator interface for sending requests and publishing notifications
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Sends a request to a single handler
    /// </summary>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="request">The request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The response</returns>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a notification to all handlers
    /// </summary>
    /// <param name="notification">The notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task Publish(INotification notification, CancellationToken cancellationToken = default);
}
