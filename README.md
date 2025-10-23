# Bellows

A mediator library for .NET that decouples senders from receivers using request/response and pub/sub patterns.

[![9.0+](https://img.shields.io/badge/.NET-9.0+-512BD4)](https://dotnet.microsoft.com/)
[![NuGet Version](https://img.shields.io/nuget/v/Bellows)](https://www.nuget.org/packages/Bellows)
[![Build](https://img.shields.io/github/actions/workflow/status/Smithing-Systems/Bellows/build.yml)](https://github.com/Smithing-Systems/Bellows/actions/workflows/build.yml)
[![Tests](https://img.shields.io/github/actions/workflow/status/Smithing-Systems/Bellows/test.yml)](https://github.com/Smithing-Systems/Bellows/actions/workflows/test.yml)
[![License](https://img.shields.io/badge/license-MIT-blue)](https://github.com/Smithing-Systems/Bellows/blob/main/LICENCE.md)


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

### Registering Behaviors

Register pipeline behaviors explicitly using `AddPipelineBehavior`:

```csharp
// Register as open generic (applies to all requests)
builder.Services.AddBellows(typeof(Program).Assembly);
builder.Services.AddPipelineBehavior(typeof(LoggingBehavior<,>));
builder.Services.AddPipelineBehavior(typeof(ValidationBehavior<,>));

// Order matters - behaviors execute in registration order
```

Alternatively, closed generic behaviors are auto-registered when included in assemblies passed to `AddBellows()`.

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

Configure notification execution and exception handling:

```csharp
builder.Services.AddBellows(options =>
{
    // Execution Strategy: How handlers are executed
    // Parallel (default): All handlers run concurrently for better performance
    // Sequential: Handlers run one at a time in registration order
    options.NotificationPublishStrategy = NotificationPublishStrategy.Parallel;

    // Exception Strategy: How exceptions are handled
    // ContinueOnException (default): All handlers run, first exception rethrown
    // StopOnFirstException: Stop immediately on first exception
    // AggregateExceptions: Collect all exceptions into AggregateException
    // SuppressExceptions: Silently suppress all exceptions (use with caution)
    options.NotificationExceptionHandling = NotificationExceptionHandlingStrategy.ContinueOnException;

}, typeof(Program).Assembly);
```

**Choosing the Right Configuration:**

- **Parallel + ContinueOnException** (default): Best for performance when handlers are independent and you want all to execute despite failures.
- **Sequential + ContinueOnException**: Use when handlers have ordering dependencies or you need deterministic exception handling.
- **Sequential + StopOnFirstException**: Use when handler order matters and you want to stop immediately on any failure.
- **Parallel + AggregateExceptions**: Use when you need to know about all failures that occurred.

### Available Options

| Option | Default | Description |
|--------|---------|-------------|
| `NotificationPublishStrategy` | `Parallel` | `Parallel` or `Sequential` |
| `NotificationExceptionHandling` | `ContinueOnException` | How to handle exceptions in notification handlers |
| `RequestTimeout` | `null` | Global timeout for requests |
| `ThrowOnMissingHandler` | `true` | Throw exception if no handler found |
| `MaxConcurrentNotifications` | `null` | Limit concurrent notification handlers |

### Exception Handling Strategies

Exception handling behavior varies based on both the **exception handling strategy** and the **execution strategy** (parallel vs sequential):

| Strategy | Parallel Execution | Sequential Execution | Recommendation |
|----------|-------------------|---------------------|----------------|
| `ContinueOnException` (default) | All handlers run concurrently. If any fail, the first exception thrown (by timing) is rethrown after all complete. | Handlers run one at a time. If a handler fails, subsequent handlers still execute. The first exception encountered is rethrown after all complete. | ✅ **Recommended** - Good balance between resilience and error visibility |
| `StopOnFirstException` | All handlers start concurrently. The first exception thrown stops further execution and is immediately rethrown. | Handlers run one at a time. The first exception immediately stops execution and is rethrown. Remaining handlers don't execute. | ⚠️ **Use with caution** - May leave system in inconsistent state by skipping handlers |
| `AggregateExceptions` | All handlers run concurrently. All exceptions are collected and thrown together as `AggregateException` after all complete. | Handlers run one at a time. All exceptions are collected and thrown together as `AggregateException` after all complete. | ✅ **Recommended** - Best for comprehensive error reporting and diagnostics |
| `SuppressExceptions` | All handlers run concurrently. All exceptions are silently suppressed. | Handlers run one at a time. All exceptions are silently suppressed. | ❌ **Not recommended** - Hides errors and makes debugging difficult |

**Key Differences:**

- **Parallel + ContinueOnException**: The "first" exception is non-deterministic since handlers run concurrently. The exception thrown depends on which handler fails first by timing.
- **Sequential + ContinueOnException**: The "first" exception is deterministic - it's the exception from the first handler in registration order that throws.
- **StopOnFirstException with Parallel**: Cannot guarantee *which* handler's exception is thrown due to concurrent execution, only that execution stops as soon as one fails.
- **Sequential Execution**: Provides deterministic, predictable ordering for both success and failure cases.

**Example:**

```csharp
public record OrderCreated(int OrderId) : INotification;

public class EmailHandler : INotificationHandler<OrderCreated>
{
    public async Task Handle(OrderCreated notification, CancellationToken ct)
    {
        await Task.Delay(50);
        throw new InvalidOperationException("Email service down");
    }
}

public class LogHandler : INotificationHandler<OrderCreated>
{
    public async Task Handle(OrderCreated notification, CancellationToken ct)
    {
        await Task.Delay(10);
        throw new InvalidOperationException("Logging failed");
    }
}

// Parallel + ContinueOnException (default):
// Both handlers run concurrently. LogHandler likely throws first due to shorter delay.
// Result: "Logging failed" exception thrown (non-deterministic)

// Sequential + ContinueOnException:
// EmailHandler runs first (registration order), then LogHandler.
// Result: "Email service down" exception thrown (deterministic)

// Parallel + StopOnFirstException:
// Both start. First failure stops execution immediately.
// Result: Either exception could be thrown (race condition)

// Sequential + StopOnFirstException:
// EmailHandler runs first and throws. LogHandler never executes.
// Result: "Email service down" exception thrown

// Parallel + AggregateExceptions:
// Both run and both exceptions are collected.
// Result: AggregateException containing both exceptions

// Sequential + AggregateExceptions:
// Both run sequentially and both exceptions are collected.
// Result: AggregateException containing both exceptions (in registration order)
```

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
