<div align="center">

# Bellows

### A lightweight, powerful mediator library for .NET

Clean architecture made simple with request/response patterns, pub/sub notifications, and pipeline behaviors

[![.NET 9.0+](https://img.shields.io/badge/.NET-9.0+-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/tests-39%20passing-success)](#testing)
[![License](https://img.shields.io/badge/license-MIT-blue)](#)

[Quick Start](#quick-start) • [Features](#features) • [Documentation](#api-reference) • [Examples](#configuration-options)

</div>

---

## Why Bellows?

Like a bellows that channels air to fan flames, this library channels messages between components in your application - keeping your architecture clean and maintainable by **decoupling senders from receivers**.

```csharp
// Before: Tight coupling
public class OrderController {
    public OrderController(IEmailService email, ILogger logger, IInventory inventory) { ... }
    public async Task CreateOrder() {
        // Direct dependencies everywhere
    }
}

// After: Clean separation with Bellows
public class OrderController {
    public OrderController(IMediator mediator) { ... }
    public async Task CreateOrder() {
        await _mediator.Send(new CreateOrderCommand(...));
        await _mediator.Publish(new OrderCreated(...));
    }
}
```

## Features

✨ **Request/Response Pattern** - Send commands or queries and get strongly-typed responses
📢 **Pub/Sub Notifications** - Publish events to multiple handlers with parallel or sequential execution
🔄 **Pipeline Behaviors** - Add logging, validation, caching, and authorization as cross-cutting concerns
⚡ **High Performance** - Reflection caching and parallel execution for optimal throughput
🎛️ **Advanced Configuration** - Exception handling, timeouts, performance monitoring, concurrency limiting
🪶 **Minimal Dependencies** - Only `Microsoft.Extensions.DependencyInjection.Abstractions`
🔒 **Type-Safe** - Strongly typed requests, responses, and notifications
✅ **Production-Ready** - Comprehensive test coverage (39 tests passing) and battle-tested patterns

## Installation

Add the Bellows project reference to your solution:

```bash
dotnet add package Bellows
```

**Requirements:** .NET 9.0 or higher (no .NET Framework support)

## Quick Start

Get up and running in 3 simple steps:

### Step 1: Define your messages

```csharp
using Bellows;

// Request/Response - for queries and commands
public record GetUserQuery(int UserId) : IRequest<UserResponse>;
public record UserResponse(int Id, string Name, string Email);

// Notifications - for events (pub/sub)
public record OrderCreated(int OrderId, decimal Amount) : INotification;
```

### Step 2: Create handlers

```csharp
// Request handler - exactly one handler per request
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserResponse>
{
    public async Task<UserResponse> Handle(GetUserQuery request, CancellationToken ct)
    {
        var user = await _repository.GetByIdAsync(request.UserId, ct);
        return new UserResponse(user.Id, user.Name, user.Email);
    }
}

// Notification handlers - multiple handlers can listen to the same event
public class EmailNotificationHandler : INotificationHandler<OrderCreated>
{
    public async Task Handle(OrderCreated notification, CancellationToken ct)
        => await _emailService.SendConfirmationAsync(notification.OrderId, ct);
}

public class LoggingNotificationHandler : INotificationHandler<OrderCreated>
{
    public async Task Handle(OrderCreated notification, CancellationToken ct)
        => _logger.LogInformation("Order {Id} created: ${Amount}", notification.OrderId, notification.Amount);
}
```

### Step 3: Register and use

```csharp
// In Program.cs or Startup.cs
builder.Services.AddBellows(typeof(Program).Assembly);

// Inject IMediator anywhere
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpGet("{id}")]
    public async Task<UserResponse> GetUser(int id)
        => await _mediator.Send(new GetUserQuery(id));

    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderCommand cmd)
    {
        var orderId = await _mediator.Send(cmd);
        await _mediator.Publish(new OrderCreated(orderId, cmd.Amount));
        return Created($"/orders/{orderId}", null);
    }
}
```

That's it! Bellows automatically discovers and registers all handlers in the specified assemblies.

---

## Table of Contents

- [Core Concepts](#core-concepts)
- [Pipeline Behaviors](#pipeline-behaviors)
- [Configuration Options](#configuration-options)
- [Advanced Examples](#advanced-examples)
- [API Reference](#api-reference)
- [Testing](#testing)
- [Project Structure](#project-structure)

---

## Core Concepts

### Request/Response Pattern

The request/response pattern is ideal for **commands** and **queries** where you need exactly one handler to process the request and return a result.

```csharp
// Query example
public record GetProductByIdQuery(int ProductId) : IRequest<Product>;

public class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, Product>
{
    public async Task<Product> Handle(GetProductByIdQuery request, CancellationToken ct)
        => await _db.Products.FindAsync(request.ProductId, ct);
}

// Usage
var product = await _mediator.Send(new GetProductByIdQuery(123));
```

```csharp
// Command example
public record CreateProductCommand(string Name, decimal Price) : IRequest<int>;

public class CreateProductHandler : IRequestHandler<CreateProductCommand, int>
{
    public async Task<int> Handle(CreateProductCommand cmd, CancellationToken ct)
    {
        var product = new Product { Name = cmd.Name, Price = cmd.Price };
        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return product.Id;
    }
}

// Usage
var productId = await _mediator.Send(new CreateProductCommand("Widget", 19.99m));
```

### Notification Pattern (Pub/Sub)

The notification pattern is perfect for **events** where you want multiple handlers to react to the same message. Handlers can run in parallel (default) or sequentially.

```csharp
public record UserRegistered(int UserId, string Email) : INotification;

// Handler 1: Send welcome email
public class SendWelcomeEmailHandler : INotificationHandler<UserRegistered>
{
    public async Task Handle(UserRegistered evt, CancellationToken ct)
        => await _emailService.SendWelcomeEmailAsync(evt.Email, ct);
}

// Handler 2: Create default preferences
public class CreateUserPreferencesHandler : INotificationHandler<UserRegistered>
{
    public async Task Handle(UserRegistered evt, CancellationToken ct)
        => await _db.Preferences.AddAsync(new Preferences { UserId = evt.UserId }, ct);
}

// Handler 3: Log the event
public class LogUserRegisteredHandler : INotificationHandler<UserRegistered>
{
    public async Task Handle(UserRegistered evt, CancellationToken ct)
        => _logger.LogInformation("User {UserId} registered", evt.UserId);
}

// Usage - all three handlers execute (in parallel by default)
await _mediator.Publish(new UserRegistered(userId, email));
```

## Pipeline Behaviors

Add cross-cutting concerns that wrap around your request handlers - perfect for **logging**, **validation**, **caching**, **authorization**, and more.

**Note**: Behaviors execute before handlers and can short-circuit the pipeline. They do not provide post-processing hooks after the handler completes.

### Example: Logging Behavior

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing {Request}", typeof(TRequest).Name);
        var response = await next(); // Call the actual handler
        _logger.LogInformation("Completed {Request}", typeof(TRequest).Name);
        return response;
    }
}
```

### Example: Validation Behavior

```csharp
// Validate requests before they reach the handler
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var failures = _validators
            .Select(v => v.Validate(request))
            .SelectMany(result => result.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}
```

Behaviors are automatically registered when you call `AddBellows()` if they're in the scanned assemblies.

<details>
<summary><b>More Behavior Examples (click to expand)</b></summary>

#### Authorization Behavior

```csharp
public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IAuthorizedRequest
{
    private readonly ICurrentUserService _currentUser;

    public AuthorizationBehavior(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(request.RequiredPermission))
        {
            throw new UnauthorizedAccessException("User does not have required permission");
        }

        return await next();
    }
}
```

#### Caching Behavior

```csharp
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ICacheableRequest
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var cached = await _cache.GetStringAsync(request.CacheKey, cancellationToken);
        if (cached != null)
            return JsonSerializer.Deserialize<TResponse>(cached)!;

        var response = await next();
        await _cache.SetStringAsync(request.CacheKey, JsonSerializer.Serialize(response), cancellationToken);
        return response;
    }
}
```

</details>

## Configuration Options

Bellows offers powerful configuration to fine-tune behavior:

### Basic Configuration

```csharp
builder.Services.AddBellows(options =>
{
    // Parallel execution (default) or sequential
    options.NotificationPublishStrategy = NotificationPublishStrategy.Parallel;

    // How to handle exceptions in notification handlers
    options.NotificationExceptionHandling = NotificationExceptionHandlingStrategy.ContinueOnException;

    // Global timeout for all requests
    options.RequestTimeout = TimeSpan.FromSeconds(30);

    // Throw when no handler is found (default: true)
    options.ThrowOnMissingHandler = true;

}, typeof(Program).Assembly);
```

### Performance Monitoring

Track slow requests automatically:

```csharp
options.EnablePerformanceMonitoring = true;
options.PerformanceThresholdMs = 500; // Log if request takes > 500ms
options.OnPerformanceThresholdExceeded = (requestName, elapsedMs) =>
    _logger.LogWarning("{Request} took {Elapsed}ms", requestName, elapsedMs);
```

### Concurrency Control

Limit concurrent notification handlers in parallel mode:

```csharp
options.MaxConcurrentNotifications = 20; // Max 20 handlers running at once
```

### Configuration Reference

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `NotificationPublishStrategy` | Enum | `Parallel` | `Parallel` or `Sequential` execution |
| `NotificationExceptionHandling` | Enum | `ContinueOnException` | How to handle handler exceptions |
| `RequestTimeout` | `TimeSpan?` | `null` | Global timeout for requests |
| `ThrowOnMissingHandler` | `bool` | `true` | Throw if no handler found |
| `EnablePerformanceMonitoring` | `bool` | `false` | Track slow requests |
| `PerformanceThresholdMs` | `int` | `1000` | Threshold for slow requests (ms) |
| `MaxConcurrentNotifications` | `int?` | `null` | Limit concurrent handlers |

<details>
<summary><b>Exception Handling Strategies</b></summary>

- **ContinueOnException** (default): All handlers run, first exception thrown at the end
- **StopOnFirstException**: Stop execution on first exception
- **AggregateExceptions**: Collect all exceptions into `AggregateException`
- **SuppressExceptions**: Swallow all exceptions (use with caution)

</details>

---

## Advanced Examples

### Clean Architecture Integration

```csharp
// Domain Layer - Pure domain logic, no dependencies
public record CreateOrderCommand(int CustomerId, List<OrderItem> Items) : IRequest<int>;

// Application Layer - Business logic and orchestration
public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, int>
{
    private readonly IOrderRepository _orders;
    private readonly IMediator _mediator; // Can publish domain events

    public async Task<int> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        var order = new Order(cmd.CustomerId, cmd.Items);
        await _orders.AddAsync(order, ct);
        await _mediator.Publish(new OrderCreated(order.Id, order.Total), ct);
        return order.Id;
    }
}

// Infrastructure Layer - Notification handlers
public class SendOrderEmailHandler : INotificationHandler<OrderCreated>
{
    public async Task Handle(OrderCreated evt, CancellationToken ct)
        => await _emailService.SendOrderConfirmationAsync(evt.OrderId, ct);
}
```

### CQRS Pattern

```csharp
// Commands - Modify state
public record CreateUserCommand(string Email, string Name) : IRequest<int>;
public record UpdateUserCommand(int UserId, string Name) : IRequest;

// Queries - Read state
public record GetUserByIdQuery(int UserId) : IRequest<UserDto>;
public record GetUsersQuery(int PageSize, int Page) : IRequest<List<UserDto>>;

// Separate read and write models
public class CreateUserHandler : IRequestHandler<CreateUserCommand, int> { ... }
public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, UserDto> { ... }
```

### Event Sourcing

```csharp
public record OrderPlaced(int OrderId, decimal Total) : INotification;
public record OrderShipped(int OrderId, string TrackingNumber) : INotification;
public record OrderCancelled(int OrderId, string Reason) : INotification;

// Event store handler
public class EventStoreHandler :
    INotificationHandler<OrderPlaced>,
    INotificationHandler<OrderShipped>,
    INotificationHandler<OrderCancelled>
{
    public async Task Handle(OrderPlaced evt, CancellationToken ct)
        => await _eventStore.AppendAsync("Order", evt.OrderId, evt, ct);

    public async Task Handle(OrderShipped evt, CancellationToken ct)
        => await _eventStore.AppendAsync("Order", evt.OrderId, evt, ct);

    public async Task Handle(OrderCancelled evt, CancellationToken ct)
        => await _eventStore.AppendAsync("Order", evt.OrderId, evt, ct);
}
```

---

## API Reference

### Core Interfaces

| Interface | Purpose | Key Methods |
|-----------|---------|-------------|
| `IRequest<TResponse>` | Marker for requests that return a response | - |
| `IRequestHandler<TRequest, TResponse>` | Handler for requests | `Task<TResponse> Handle(TRequest, CancellationToken)` |
| `INotification` | Marker for notifications (events) | - |
| `INotificationHandler<TNotification>` | Handler for notifications | `Task Handle(TNotification, CancellationToken)` |
| `IPipelineBehavior<TRequest, TResponse>` | Cross-cutting concerns | `Task<TResponse> Handle(TRequest, RequestHandlerDelegate<TResponse>, CancellationToken)` |
| `IMediator` | Main mediator interface | `Task<TResponse> Send<TResponse>(IRequest<TResponse>)`<br>`Task Publish(INotification)` |

### Registration Methods

```csharp
// Basic registration
services.AddBellows(typeof(Program).Assembly);

// With configuration
services.AddBellows(options => { ... }, typeof(Program).Assembly);

// Add custom pipeline behavior
services.AddPipelineBehavior<MyBehavior<TRequest, TResponse>>();
```

---

## Testing

**39 tests, all passing ✅**

Comprehensive test coverage including:
- Request/response patterns
- Pub/sub notifications
- Pipeline behaviors
- Exception handling strategies
- Performance monitoring
- Timeout enforcement
- Parallel vs sequential execution
- Concurrency limiting
- DI integration

```bash
dotnet test
```

### Mocking in Unit Tests

```csharp
// Mock the mediator for testing
var mockMediator = new Mock<IMediator>();
mockMediator
    .Setup(m => m.Send(It.IsAny<GetUserQuery>(), default))
    .ReturnsAsync(new UserResponse(1, "John", "john@example.com"));

var controller = new UsersController(mockMediator.Object);
var result = await controller.GetUser(1);

Assert.Equal("John", result.Name);
```

---

## Project Structure

<details>
<summary><b>View project layout</b></summary>

```
Bellows/
├── Bellows/                               # Main library
│   ├── Abstractions/                      # Core interfaces
│   │   ├── IRequest.cs
│   │   ├── IRequestHandler.cs
│   │   ├── INotification.cs
│   │   ├── INotificationHandler.cs
│   │   ├── IPipelineBehavior.cs
│   │   ├── IMediator.cs
│   │   └── MediatorOptions.cs
│   ├── Implementation/
│   │   └── Mediator.cs
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs
├── Bellows.Tests/                         # Test project
│   ├── Handlers/
│   └── Tests/
└── README.md
```

</details>

---

## Design Principles

🎯 **Simple** - Minimal API surface, easy to understand
⚡ **Fast** - Reflection caching for optimal performance
🪶 **Lightweight** - Single dependency on DI abstractions
🧪 **Testable** - Easy to mock and unit test
🔧 **Flexible** - Extensive configuration options
✅ **Production-Ready** - Battle-tested patterns with comprehensive error handling

---

## License

MIT

---

<div align="center">

**Made with ❤️ by [Smithing Systems](https://smithingsystems.com)**

</div>
