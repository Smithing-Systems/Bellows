using System.Reflection;
using Bellows.Abstractions;
using Bellows.Extensions;
using Bellows.Tests.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Bellows.Tests.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddBellows_RegistersMediator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBellows(typeof(ServiceCollectionExtensionsTests).Assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        var mediator = provider.GetService<IMediator>();
        Assert.NotNull(mediator);
        Assert.IsType<Mediator>(mediator);
    }

    [Fact]
    public void AddBellows_RegistersRequestHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBellows(typeof(GetUserQueryHandler).Assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        var handler = provider.GetService<IRequestHandler<GetUserQuery, UserResponse>>();
        Assert.NotNull(handler);
        Assert.IsType<GetUserQueryHandler>(handler);
    }

    [Fact]
    public void AddBellows_RegistersNotificationHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBellows(typeof(OrderCreatedEmailHandler).Assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        var handlers = provider.GetServices<INotificationHandler<OrderCreatedNotification>>().ToList();
        Assert.NotEmpty(handlers);
        Assert.Contains(handlers, h => h is OrderCreatedEmailHandler);
        Assert.Contains(handlers, h => h is OrderCreatedLoggingHandler);
        Assert.Contains(handlers, h => h is OrderCreatedInventoryHandler);
    }

    [Fact]
    public void AddBellows_WithNoAssemblies_UsesCallingAssembly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBellows();
        var provider = services.BuildServiceProvider();

        // Assert
        var mediator = provider.GetService<IMediator>();
        Assert.NotNull(mediator);
    }

    [Fact]
    public void AddBellows_WithMultipleAssemblies_RegistersHandlersFromAll()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly1 = typeof(GetUserQueryHandler).Assembly;
        var assembly2 = typeof(Mediator).Assembly;

        // Act
        services.AddBellows(assembly1, assembly2);
        var provider = services.BuildServiceProvider();

        // Assert
        var mediator = provider.GetService<IMediator>();
        var handler = provider.GetService<IRequestHandler<GetUserQuery, UserResponse>>();
        Assert.NotNull(mediator);
        Assert.NotNull(handler);
    }

    [Fact]
    public void AddBellows_WithNullServiceCollection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ServiceCollectionExtensions.AddBellows(null!, Assembly.GetExecutingAssembly()));
    }

    [Fact]
    public void AddBellows_RegistersHandlersAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBellows(typeof(GetUserQueryHandler).Assembly);
        var provider = services.BuildServiceProvider();

        // Assert - getting the service twice should return different instances
        var handler1 = provider.GetService<IRequestHandler<GetUserQuery, UserResponse>>();
        var handler2 = provider.GetService<IRequestHandler<GetUserQuery, UserResponse>>();

        Assert.NotNull(handler1);
        Assert.NotNull(handler2);
        Assert.NotSame(handler1, handler2);
    }
}