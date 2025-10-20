namespace Bellows.Abstractions;

/// <summary>
/// Handler for a notification
/// </summary>
/// <typeparam name="TNotification">The notification type</typeparam>
public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    /// <summary>
    /// Handles the notification
    /// </summary>
    /// <param name="notification">The notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task Handle(TNotification notification, CancellationToken cancellationToken = default);
}
