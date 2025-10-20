using Bellows.Abstractions;
using Bellows.Extensions;
using Bellows.Tests.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Bellows.Tests.Tests;

public class NotificationExecutionStrategyTests
{
    [Fact]
    public async Task Publish_WithParallelStrategy_ExecutesHandlersInParallel()
    {
        // Arrange
        OrderedHandler1.ExecutionLog.Clear();
        OrderedHandler2.ExecutionLog.Clear();
        OrderedHandler3.ExecutionLog.Clear();

        var services = new ServiceCollection();
        services.AddBellows(options =>
        {
            options.NotificationPublishStrategy = NotificationPublishStrategy.Parallel;
        }, typeof(NotificationExecutionStrategyTests).Assembly);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var notification = new OrderedNotification("Test");

        // Act
        var startTime = DateTime.UtcNow;
        await mediator.Publish(notification);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        // In parallel mode, all handlers should start before any of them finish
        // So we should see interleaved start/end logs
        var allLogs = new List<string>();
        allLogs.AddRange(OrderedHandler1.ExecutionLog);
        allLogs.AddRange(OrderedHandler2.ExecutionLog);
        allLogs.AddRange(OrderedHandler3.ExecutionLog);

        Assert.Contains("Handler1-Start", allLogs);
        Assert.Contains("Handler2-Start", allLogs);
        Assert.Contains("Handler3-Start", allLogs);
        Assert.Contains("Handler1-End", allLogs);
        Assert.Contains("Handler2-End", allLogs);
        Assert.Contains("Handler3-End", allLogs);

        // Parallel execution should take roughly the time of the longest handler (50ms)
        // With some buffer for execution overhead (100ms max)
        Assert.True(duration.TotalMilliseconds < 100,
            $"Parallel execution took {duration.TotalMilliseconds}ms, expected < 100ms");
    }

    [Fact]
    public async Task Publish_WithSequentialStrategy_ExecutesHandlersInOrder()
    {
        // Arrange
        OrderedHandler1.ExecutionLog.Clear();
        OrderedHandler2.ExecutionLog.Clear();
        OrderedHandler3.ExecutionLog.Clear();

        var services = new ServiceCollection();
        services.AddBellows(options =>
        {
            options.NotificationPublishStrategy = NotificationPublishStrategy.Sequential;
        }, typeof(NotificationExecutionStrategyTests).Assembly);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var notification = new OrderedNotification("Test");

        // Act
        var startTime = DateTime.UtcNow;
        await mediator.Publish(notification);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        // In sequential mode, each handler should complete before the next starts
        var allLogs = new List<string>();
        allLogs.AddRange(OrderedHandler1.ExecutionLog);
        allLogs.AddRange(OrderedHandler2.ExecutionLog);
        allLogs.AddRange(OrderedHandler3.ExecutionLog);

        Assert.Equal(6, allLogs.Count);

        // Verify sequential execution order
        Assert.Equal("Handler1-Start", allLogs[0]);
        Assert.Equal("Handler1-End", allLogs[1]);
        Assert.Equal("Handler2-Start", allLogs[2]);
        Assert.Equal("Handler2-End", allLogs[3]);
        Assert.Equal("Handler3-Start", allLogs[4]);
        Assert.Equal("Handler3-End", allLogs[5]);

        // Sequential execution should take the sum of all handlers (50 + 30 + 20 = 100ms)
        // Allow some overhead (150ms max)
        Assert.True(duration.TotalMilliseconds >= 90,
            $"Sequential execution took {duration.TotalMilliseconds}ms, expected >= 90ms");
        Assert.True(duration.TotalMilliseconds < 150,
            $"Sequential execution took {duration.TotalMilliseconds}ms, expected < 150ms");
    }

    [Fact]
    public async Task Publish_WithDefaultOptions_UsesParallelStrategy()
    {
        // Arrange
        OrderedHandler1.ExecutionLog.Clear();
        OrderedHandler2.ExecutionLog.Clear();
        OrderedHandler3.ExecutionLog.Clear();

        var services = new ServiceCollection();
        services.AddBellows(typeof(NotificationExecutionStrategyTests).Assembly);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var notification = new OrderedNotification("Test");

        // Act
        var startTime = DateTime.UtcNow;
        await mediator.Publish(notification);
        var duration = DateTime.UtcNow - startTime;

        // Assert - default should be parallel
        Assert.True(duration.TotalMilliseconds < 100,
            $"Default execution took {duration.TotalMilliseconds}ms, expected parallel (< 100ms)");
    }

    [Fact]
    public void MediatorOptions_DefaultStrategy_IsParallel()
    {
        // Arrange & Act
        var options = new MediatorOptions();

        // Assert
        Assert.Equal(NotificationPublishStrategy.Parallel, options.NotificationPublishStrategy);
    }

    [Fact]
    public void AddBellows_WithOptionsConfiguration_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBellows(options =>
        {
            options.NotificationPublishStrategy = NotificationPublishStrategy.Sequential;
        }, typeof(NotificationExecutionStrategyTests).Assembly);

        var provider = services.BuildServiceProvider();
        var mediatorOptions = provider.GetRequiredService<MediatorOptions>();

        // Assert
        Assert.NotNull(mediatorOptions);
        Assert.Equal(NotificationPublishStrategy.Sequential, mediatorOptions.NotificationPublishStrategy);
    }

    [Fact]
    public void AddBellows_WithoutOptionsConfiguration_UsesDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBellows(typeof(NotificationExecutionStrategyTests).Assembly);

        var provider = services.BuildServiceProvider();
        var mediatorOptions = provider.GetRequiredService<MediatorOptions>();

        // Assert
        Assert.NotNull(mediatorOptions);
        Assert.Equal(NotificationPublishStrategy.Parallel, mediatorOptions.NotificationPublishStrategy);
    }
}