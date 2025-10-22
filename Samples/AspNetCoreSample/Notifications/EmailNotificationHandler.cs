using Bellows.Abstractions;

namespace AspNetCoreSample;

/// <summary>
/// Sends email when an order is created
/// </summary>
public class EmailNotificationHandler : INotificationHandler<OrderCreatedNotification>
{
    private readonly ILogger<EmailNotificationHandler> _logger;

    public EmailNotificationHandler(ILogger<EmailNotificationHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(OrderCreatedNotification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending email notification for order {OrderId}", notification.OrderId);

        // Simulate sending email
        await Task.Delay(50, cancellationToken);

        _logger.LogInformation("Email sent successfully for order {OrderId}", notification.OrderId);
    }
}
