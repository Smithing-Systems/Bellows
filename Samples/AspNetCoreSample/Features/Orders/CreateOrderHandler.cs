using Bellows.Abstractions;

namespace AspNetCoreSample;

/// <summary>
/// Handles CreateOrderRequest by creating a new order
/// </summary>
public class CreateOrderHandler : IRequestHandler<CreateOrderRequest, CreateOrderResponse>
{
    private readonly ILogger<CreateOrderHandler> _logger;

    public CreateOrderHandler(ILogger<CreateOrderHandler> logger)
    {
        _logger = logger;
    }

    public async Task<CreateOrderResponse> Handle(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing order for {ProductName}, Quantity: {Quantity}",
            request.ProductName, request.Quantity);

        // Simulate some async work
        await Task.Delay(100, cancellationToken);

        var orderId = Guid.NewGuid();
        var totalPrice = request.Price * request.Quantity;

        _logger.LogInformation("Order {OrderId} created successfully", orderId);

        return new CreateOrderResponse(
            orderId,
            request.ProductName,
            request.Quantity,
            totalPrice,
            DateTime.UtcNow
        );
    }
}
