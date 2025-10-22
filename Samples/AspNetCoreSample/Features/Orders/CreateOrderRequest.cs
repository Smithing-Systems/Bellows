using Bellows.Abstractions;

namespace AspNetCoreSample;

/// <summary>
/// Request to create a new order
/// </summary>
public record CreateOrderRequest(string ProductName, int Quantity, decimal Price) : IRequest<CreateOrderResponse>;

/// <summary>
/// Response containing the created order details
/// </summary>
public record CreateOrderResponse(Guid OrderId, string ProductName, int Quantity, decimal TotalPrice, DateTime CreatedAt);
