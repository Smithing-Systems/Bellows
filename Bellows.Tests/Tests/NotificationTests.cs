using Bellows.Abstractions;
using Bellows.Extensions;
using Bellows.Tests.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Bellows.Tests.Tests;

public class NotificationTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public NotificationTests()
    {
        // Reset static counters
        OrderCreatedEmailHandler.HandleCount = 0;
        OrderCreatedLoggingHandler.HandleCount = 0;
        OrderCreatedInventoryHandler.HandleCount = 0;
        UserRegisteredHandler.LastEmail = null;

        var services = new ServiceCollection();
        services.AddBellows(typeof(NotificationTests).Assembly);

        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Publish_WithMultipleHandlers_CallsAllHandlers()
    {
        // Arrange
        var notification = new OrderCreatedNotification(123, 99.99m);

        // Act
        await _mediator.Publish(notification);

        // Assert
        Assert.Equal(1, OrderCreatedEmailHandler.HandleCount);
        Assert.Equal(1, OrderCreatedLoggingHandler.HandleCount);
        Assert.Equal(1, OrderCreatedInventoryHandler.HandleCount);
    }

    [Fact]
    public async Task Publish_WithSingleHandler_CallsHandler()
    {
        // Arrange
        var notification = new UserRegisteredNotification("test@example.com");

        // Act
        await _mediator.Publish(notification);

        // Assert
        Assert.Equal("test@example.com", UserRegisteredHandler.LastEmail);
    }

    [Fact]
    public async Task Publish_MultipleTimes_CallsHandlersMultipleTimes()
    {
        // Arrange
        var notification1 = new OrderCreatedNotification(1, 10.00m);
        var notification2 = new OrderCreatedNotification(2, 20.00m);

        // Act
        await _mediator.Publish(notification1);
        await _mediator.Publish(notification2);

        // Assert
        Assert.Equal(2, OrderCreatedEmailHandler.HandleCount);
        Assert.Equal(2, OrderCreatedLoggingHandler.HandleCount);
        Assert.Equal(2, OrderCreatedInventoryHandler.HandleCount);
    }

    [Fact]
    public async Task Publish_WithCancellationToken_PassesCancellationToken()
    {
        // Arrange
        var notification = new OrderCreatedNotification(123, 99.99m);
        var cts = new CancellationTokenSource();

        // Act
        await _mediator.Publish(notification, cts.Token);

        // Assert
        Assert.Equal(1, OrderCreatedEmailHandler.HandleCount);
    }

    [Fact]
    public async Task Publish_WithNullNotification_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _mediator.Publish(null!));
    }

    [Fact]
    public async Task Publish_WithNoRegisteredHandlers_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var notification = new OrderCreatedNotification(1, 10.00m);

        // Act & Assert - should not throw
        await mediator.Publish(notification);
    }

    [Fact]
    public async Task Publish_DifferentNotificationTypes_CallsCorrectHandlers()
    {
        // Arrange
        var orderNotification = new OrderCreatedNotification(456, 50.00m);
        var userNotification = new UserRegisteredNotification("new@example.com");

        // Act
        await _mediator.Publish(orderNotification);
        await _mediator.Publish(userNotification);

        // Assert
        Assert.Equal(1, OrderCreatedEmailHandler.HandleCount);
        Assert.Equal("new@example.com", UserRegisteredHandler.LastEmail);
    }
}