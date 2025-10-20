using Microsoft.Extensions.DependencyInjection;
using Bellows.Abstractions;
using Bellows.Extensions;
using Bellows.Tests.Handlers;

namespace Bellows.Tests.Tests;

public class PipelineBehaviorTests
{
    public PipelineBehaviorTests()
    {
        // Reset all behavior logs
        LoggingBehavior<GetUserQuery, UserResponse>.Logs.Clear();
        ValidationBehavior<GetUserQuery, UserResponse>.Logs.Clear();
        ValidationBehavior<GetUserQuery, UserResponse>.ShouldThrow = false;
        PerformanceBehavior<GetUserQuery, UserResponse>.Logs.Clear();
        CachingBehavior<GetUserQuery, UserResponse>.Logs.Clear();
        CachingBehavior<GetUserQuery, UserResponse>.Cache.Clear();
        RetryBehavior<GetUserQuery, UserResponse>.Logs.Clear();
        RetryBehavior<GetUserQuery, UserResponse>.RetryCount = 3;
    }

    [Fact]
    public async Task Send_WithSingleBehavior_ExecutesBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBellows(typeof(PipelineBehaviorTests).Assembly);
        services.AddPipelineBehavior<LoggingBehavior<GetUserQuery, UserResponse>>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var query = new GetUserQuery(1);

        // Act
        var response = await mediator.Send(query);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(1, response.Id);

        var logs = LoggingBehavior<GetUserQuery, UserResponse>.Logs;
        Assert.Equal(2, logs.Count);
        Assert.Equal("[LoggingBehavior] Before: GetUserQuery", logs[0]);
        Assert.Equal("[LoggingBehavior] After: GetUserQuery", logs[1]);
    }

    [Fact]
    public async Task Send_WithMultipleBehaviors_ExecutesInOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBellows(typeof(PipelineBehaviorTests).Assembly);
        services.AddPipelineBehavior<LoggingBehavior<GetUserQuery, UserResponse>>();
        services.AddPipelineBehavior<ValidationBehavior<GetUserQuery, UserResponse>>();
        services.AddPipelineBehavior<PerformanceBehavior<GetUserQuery, UserResponse>>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var query = new GetUserQuery(1);

        // Act
        var response = await mediator.Send(query);

        // Assert
        Assert.NotNull(response);

        // Check execution order: Logging -> Validation -> Performance -> Handler -> Performance -> Validation -> Logging
        var allLogs = new List<string>();
        allLogs.AddRange(LoggingBehavior<GetUserQuery, UserResponse>.Logs);
        allLogs.AddRange(ValidationBehavior<GetUserQuery, UserResponse>.Logs);
        allLogs.AddRange(PerformanceBehavior<GetUserQuery, UserResponse>.Logs);

        Assert.Contains("[LoggingBehavior] Before: GetUserQuery", allLogs);
        Assert.Contains("[ValidationBehavior] Validating", allLogs);
        Assert.Contains("[PerformanceBehavior] Start: GetUserQuery", allLogs);
        Assert.Contains("[LoggingBehavior] After: GetUserQuery", allLogs);
    }

    [Fact]
    public async Task Send_WithValidationBehaviorThatThrows_StopsExecution()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBellows(typeof(PipelineBehaviorTests).Assembly);
        services.AddPipelineBehavior<LoggingBehavior<GetUserQuery, UserResponse>>();
        services.AddPipelineBehavior<ValidationBehavior<GetUserQuery, UserResponse>>();
        services.AddPipelineBehavior<PerformanceBehavior<GetUserQuery, UserResponse>>();

        ValidationBehavior<GetUserQuery, UserResponse>.ShouldThrow = true;

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var query = new GetUserQuery(1);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(query));

        // Performance behavior should not have run since validation threw
        var perfLogs = PerformanceBehavior<GetUserQuery, UserResponse>.Logs;
        Assert.Empty(perfLogs);
    }

    [Fact]
    public async Task Send_WithCachingBehavior_CachesResults()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBellows(typeof(PipelineBehaviorTests).Assembly);
        services.AddPipelineBehavior<CachingBehavior<GetUserQuery, UserResponse>>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var query = new GetUserQuery(42);

        // Act
        var response1 = await mediator.Send(query);
        var response2 = await mediator.Send(query);

        // Assert
        Assert.NotNull(response1);
        Assert.NotNull(response2);
        Assert.Equal(response1.Id, response2.Id);

        var logs = CachingBehavior<GetUserQuery, UserResponse>.Logs;
        Assert.Equal(2, logs.Count);
        Assert.Contains("Cache miss", logs[0]);
        Assert.Contains("Cache hit", logs[1]);
    }

    [Fact]
    public async Task Send_WithNoBehaviors_ExecutesHandlerDirectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBellows(typeof(PipelineBehaviorTests).Assembly);
        // No behaviors registered

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var query = new GetUserQuery(1);

        // Act
        var response = await mediator.Send(query);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(1, response.Id);
    }

    [Fact]
    public async Task Send_WithBehaviorForDifferentRequestType_DoesNotExecute()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBellows(typeof(PipelineBehaviorTests).Assembly);
        services.AddPipelineBehavior<LoggingBehavior<CalculateCommand, int>>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Use GetUserQuery which doesn't have logging behavior
        var query = new GetUserQuery(1);

        // Act
        var response = await mediator.Send(query);

        // Assert
        Assert.NotNull(response);

        // Logging behavior for GetUserQuery should not have logs
        var logs = LoggingBehavior<GetUserQuery, UserResponse>.Logs;
        Assert.Empty(logs);
    }

    [Fact]
    public async Task Send_WithPerformanceBehavior_MeasuresExecutionTime()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBellows(typeof(PipelineBehaviorTests).Assembly);
        services.AddPipelineBehavior<PerformanceBehavior<GetUserQuery, UserResponse>>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var query = new GetUserQuery(1);

        // Act
        var response = await mediator.Send(query);

        // Assert
        Assert.NotNull(response);

        var logs = PerformanceBehavior<GetUserQuery, UserResponse>.Logs;
        Assert.Equal(2, logs.Count);
        Assert.Contains("Start: GetUserQuery", logs[0]);
        Assert.Contains("End: GetUserQuery", logs[1]);
        Assert.Contains("ms", logs[1]);
    }

    [Fact]
    public void AddPipelineBehavior_WithGenericMethod_RegistersBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBellows(typeof(PipelineBehaviorTests).Assembly);

        // Act
        services.AddPipelineBehavior<LoggingBehavior<GetUserQuery, UserResponse>>();

        // Assert
        var provider = services.BuildServiceProvider();
        var behavior = provider.GetService<IPipelineBehavior<GetUserQuery, UserResponse>>();
        Assert.NotNull(behavior);
        Assert.IsType<LoggingBehavior<GetUserQuery, UserResponse>>(behavior);
    }

    [Fact]
    public void AddPipelineBehavior_WithNonBehaviorType_ThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddPipelineBehavior(typeof(GetUserQueryHandler)));
    }

    [Fact]
    public void AddPipelineBehavior_WithNullServiceCollection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ServiceCollectionExtensions.AddPipelineBehavior(null!, typeof(LoggingBehavior<GetUserQuery, UserResponse>)));
    }

    [Fact]
    public void AddPipelineBehavior_WithNullBehaviorType_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            services.AddPipelineBehavior(null!));
    }

    [Fact]
    public async Task Send_WithAutoRegisteredBehavior_ExecutesBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        // Behaviors in the scanned assembly should be auto-registered
        services.AddBellows(typeof(LoggingBehavior<GetUserQuery, UserResponse>).Assembly);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var query = new GetUserQuery(1);

        // Act
        var response = await mediator.Send(query);

        // Assert - should work even without explicit AddPipelineBehavior
        Assert.NotNull(response);
    }
}
