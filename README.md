# Bellows

A mediator library for .NET that decouples senders from receivers using request/response and pub/sub patterns.

[![.NET 9.0+](https://img.shields.io/badge/.NET-9.0+-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue)](#)

## Installation

```bash
dotnet add package Bellows
```

**Requirements:** .NET 9.0 or higher

## Getting Started

### 1. Register Bellows

In your `Program.cs`:

```csharp
builder.Services.AddBellows(typeof(Program).Assembly);
```

This registers the mediator and automatically discovers all handlers in the specified assembly.

### 2. Create a Request and Handler

Define a request that returns a response:

```csharp
using Bellows;

public record GetUserQuery(int UserId) : IRequest<UserResponse>;

public record UserResponse(int Id, string Name, string Email);
```

Create a handler for the request:

```csharp
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserResponse>
{
    private readonly IUserRepository _repository;

    public GetUserQueryHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserResponse> Handle(GetUserQuery request, CancellationToken ct)
    {
        var user = await _repository.GetByIdAsync(request.UserId, ct);
        return new UserResponse(user.Id, user.Name, user.Email);
    }
}
```

### 3. Send the Request

Inject `IMediator` and send your request:

```csharp
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id}")]
    public async Task<UserResponse> GetUser(int id)
    {
        return await _mediator.Send(new GetUserQuery(id));
    }
}
```

## Requests vs Notifications

### Requests (One Handler)

Use requests when you need exactly one handler to process a message and return a result.

```csharp
// Define the request
public record CreateOrderCommand(int CustomerId, decimal Amount) : IRequest<int>;

// Create the handler
public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, int>
{
    public async Task<int> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        var order = new Order { CustomerId = cmd.CustomerId, Amount = cmd.Amount };
        await _db.Orders.AddAsync(order, ct);
        await _db.SaveChangesAsync(ct);
        return order.Id;
    }
}

// Send the request
var orderId = await _mediator.Send(new CreateOrderCommand(123, 99.99m));
```

### Notifications (Multiple Handlers)

Use notifications when you want multiple handlers to react to an event.

```csharp
// Define the notification
public record OrderCreated(int OrderId, decimal Amount) : INotification;

// Create multiple handlers
public class SendEmailHandler : INotificationHandler<OrderCreated>
{
    public async Task Handle(OrderCreated notification, CancellationToken ct)
    {
        await _emailService.SendConfirmationAsync(notification.OrderId, ct);
    }
}

public class LogOrderHandler : INotificationHandler<OrderCreated>
{
    public async Task Handle(OrderCreated notification, CancellationToken ct)
    {
        _logger.LogInformation("Order {Id} created: ${Amount}",
            notification.OrderId, notification.Amount);
    }
}

// Publish the notification (all handlers execute)
await _mediator.Publish(new OrderCreated(orderId, 99.99m));
```

## Table of Contents

- [Pipeline Behaviors](#pipeline-behaviors)
- [Configuration](#configuration)
- [API Reference](#api-reference)

## Pipeline Behaviors

Pipeline behaviors wrap around request handlers to add cross-cutting concerns like logging, validation, or caching.

### Creating a Behavior

Implement `IPipelineBehavior<TRequest, TResponse>`:

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {RequestName}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Handled {RequestName}", typeof(TRequest).Name);
        return response;
    }
}
```

Behaviors are automatically registered when included in the assemblies passed to `AddBellows()`.

### Validation Example

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(result => result.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}
```

## Configuration

### Basic Options

```csharp
builder.Services.AddBellows(options =>
{
    // Run notification handlers in parallel (default) or sequential
    options.NotificationPublishStrategy = NotificationPublishStrategy.Parallel;

    // Continue running handlers if one throws (default)
    options.NotificationExceptionHandling = NotificationExceptionHandlingStrategy.ContinueOnException;

}, typeof(Program).Assembly);
```

### Available Options

| Option | Default | Description |
|--------|---------|-------------|
| `NotificationPublishStrategy` | `Parallel` | `Parallel` or `Sequential` |
| `NotificationExceptionHandling` | `ContinueOnException` | How to handle exceptions in notification handlers |
| `RequestTimeout` | `null` | Global timeout for requests |
| `ThrowOnMissingHandler` | `true` | Throw exception if no handler found |
| `MaxConcurrentNotifications` | `null` | Limit concurrent notification handlers |

### Exception Handling Strategies

- `ContinueOnException` - Run all handlers, throw first exception at the end
- `StopOnFirstException` - Stop on first exception
- `AggregateExceptions` - Collect all exceptions into `AggregateException`
- `SuppressExceptions` - Swallow exceptions (use with caution)

## API Reference

### Core Interfaces

**`IMediator`** - Main interface for sending requests and publishing notifications

```csharp
Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification;
```

**`IRequest<TResponse>`** - Marker interface for requests

```csharp
public record GetUserQuery(int UserId) : IRequest<UserResponse>;
```

**`IRequestHandler<TRequest, TResponse>`** - Handler for requests

```csharp
public class GetUserHandler : IRequestHandler<GetUserQuery, UserResponse>
{
    Task<UserResponse> Handle(GetUserQuery request, CancellationToken cancellationToken);
}
```

**`INotification`** - Marker interface for notifications

```csharp
public record UserCreated(int UserId) : INotification;
```

**`INotificationHandler<TNotification>`** - Handler for notifications

```csharp
public class UserCreatedHandler : INotificationHandler<UserCreated>
{
    Task Handle(UserCreated notification, CancellationToken cancellationToken);
}
```

**`IPipelineBehavior<TRequest, TResponse>`** - Cross-cutting concerns

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
```

---

**Made with ❤️ by [Smithing Systems](https://smithingsystems.com)**
