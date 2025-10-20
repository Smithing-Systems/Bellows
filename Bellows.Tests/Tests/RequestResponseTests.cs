using Bellows.Abstractions;
using Bellows.Extensions;
using Bellows.Tests.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Bellows.Tests.Tests;

public class RequestResponseTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public RequestResponseTests()
    {
        var services = new ServiceCollection();
        services.AddBellows(typeof(RequestResponseTests).Assembly);

        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Send_WithValidRequest_ReturnsExpectedResponse()
    {
        // Arrange
        var query = new GetUserQuery(42);

        // Act
        var response = await _mediator.Send(query);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(42, response.Id);
        Assert.Equal("User42", response.Name);
        Assert.Equal("user42@example.com", response.Email);
    }

    [Fact]
    public async Task Send_WithCalculateCommand_ReturnsSum()
    {
        // Arrange
        var command = new CalculateCommand(10, 20);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.Equal(30, result);
    }

    [Fact]
    public async Task Send_WithCancellationToken_PassesCancellationToken()
    {
        // Arrange
        var query = new GetUserQuery(1);
        var cts = new CancellationTokenSource();

        // Act
        var response = await _mediator.Send(query, cts.Token);

        // Assert
        Assert.NotNull(response);
    }

    [Fact]
    public async Task Send_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _mediator.Send<UserResponse>(null!));
    }

    [Fact]
    public async Task Send_WithNoRegisteredHandler_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var query = new GetUserQuery(1);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(query));
    }

    [Fact]
    public void Mediator_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Mediator(null!));
    }

    [Fact]
    public async Task Send_MultipleRequests_AllReturnCorrectResults()
    {
        // Arrange
        var query1 = new GetUserQuery(1);
        var query2 = new GetUserQuery(2);
        var command = new CalculateCommand(5, 10);

        // Act
        var response1 = await _mediator.Send(query1);
        var response2 = await _mediator.Send(query2);
        var result = await _mediator.Send(command);

        // Assert
        Assert.Equal(1, response1.Id);
        Assert.Equal(2, response2.Id);
        Assert.Equal(15, result);
    }
}