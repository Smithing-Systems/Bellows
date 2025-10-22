using Bellows.Abstractions;

namespace AspNetCoreSample;

/// <summary>
/// Logs analytics when an order is created
/// </summary>
public class AnalyticsNotificationHandler : INotificationHandler<OrderCreatedNotification>
{
    private readonly ILogger<AnalyticsNotificationHandler> _logger;

    public AnalyticsNotificationHandler(ILogger<AnalyticsNotificationHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(OrderCreatedNotification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Recording analytics for order {OrderId}", notification.OrderId);

        // Simulate analytics recording
        await Task.Delay(20, cancellationToken);

        _logger.LogInformation("Analytics recorded: Order {OrderId}, Revenue ${TotalPrice}",
            notification.OrderId, notification.TotalPrice);
    }
}
