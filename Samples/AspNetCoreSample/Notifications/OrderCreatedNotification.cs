using Bellows.Abstractions;

namespace AspNetCoreSample;

/// <summary>
/// Notification published when an order is created
/// </summary>
public record OrderCreatedNotification(Guid OrderId, string ProductName, decimal TotalPrice) : INotification;
