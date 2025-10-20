using Bellows.Abstractions;

namespace Bellows.Tests.Handlers;

// Test notifications
public record OrderCreatedNotification(int OrderId, decimal Amount) : INotification;

public record UserRegisteredNotification(string Email) : INotification;

// Test notification handlers
public class OrderCreatedEmailHandler : INotificationHandler<OrderCreatedNotification>
{
    public static int HandleCount { get; set; }

    public Task Handle(OrderCreatedNotification notification, CancellationToken cancellationToken = default)
    {
        HandleCount++;
        return Task.CompletedTask;
    }
}

public class OrderCreatedLoggingHandler : INotificationHandler<OrderCreatedNotification>
{
    public static int HandleCount { get; set; }

    public Task Handle(OrderCreatedNotification notification, CancellationToken cancellationToken = default)
    {
        HandleCount++;
        return Task.CompletedTask;
    }
}

public class OrderCreatedInventoryHandler : INotificationHandler<OrderCreatedNotification>
{
    public static int HandleCount { get; set; }

    public Task Handle(OrderCreatedNotification notification, CancellationToken cancellationToken = default)
    {
        HandleCount++;
        return Task.CompletedTask;
    }
}

public class UserRegisteredHandler : INotificationHandler<UserRegisteredNotification>
{
    public static string? LastEmail { get; set; }

    public Task Handle(UserRegisteredNotification notification, CancellationToken cancellationToken = default)
    {
        LastEmail = notification.Email;
        return Task.CompletedTask;
    }
}