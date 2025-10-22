using Bellows.Abstractions;

namespace AspNetCoreSample;

/// <summary>
/// Updates inventory when an order is created
/// </summary>
public class InventoryNotificationHandler : INotificationHandler<OrderCreatedNotification>
{
    private readonly ILogger<InventoryNotificationHandler> _logger;

    public InventoryNotificationHandler(ILogger<InventoryNotificationHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(OrderCreatedNotification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating inventory for order {OrderId}", notification.OrderId);

        // Simulate inventory update
        await Task.Delay(30, cancellationToken);

        _logger.LogInformation("Inventory updated for product {ProductName}", notification.ProductName);
    }
}
