using Bellows.Abstractions;

namespace Bellows.Tests.Handlers;

// Test notification for tracking execution order
public record OrderedNotification(string Message) : INotification;

public class OrderedHandler1 : INotificationHandler<OrderedNotification>
{
    public static List<string> ExecutionLog { get; } = new();

    public async Task Handle(OrderedNotification notification, CancellationToken cancellationToken = default)
    {
        ExecutionLog.Add("Handler1-Start");
        await Task.Delay(50, cancellationToken); // Simulate work
        ExecutionLog.Add("Handler1-End");
    }
}

public class OrderedHandler2 : INotificationHandler<OrderedNotification>
{
    public static List<string> ExecutionLog { get; } = new();

    public async Task Handle(OrderedNotification notification, CancellationToken cancellationToken = default)
    {
        ExecutionLog.Add("Handler2-Start");
        await Task.Delay(30, cancellationToken); // Simulate work
        ExecutionLog.Add("Handler2-End");
    }
}

public class OrderedHandler3 : INotificationHandler<OrderedNotification>
{
    public static List<string> ExecutionLog { get; } = new();

    public async Task Handle(OrderedNotification notification, CancellationToken cancellationToken = default)
    {
        ExecutionLog.Add("Handler3-Start");
        await Task.Delay(20, cancellationToken); // Simulate work
        ExecutionLog.Add("Handler3-End");
    }
}