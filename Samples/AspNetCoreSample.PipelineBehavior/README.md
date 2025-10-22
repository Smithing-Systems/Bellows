# Bellows Pipeline Behaviors Sample

Demonstrates how to use **Pipeline Behaviors** in Bellows to add cross-cutting concerns to your requests.

## Quick Start

```bash
# Navigate to the project
cd Samples/AspNetCoreSample.PipelineBehavior

# Run the application
dotnet run

# API available at http://localhost:5000
```

## What are Pipeline Behaviors?

Pipeline behaviors wrap around request handlers, allowing you to add functionality before and/or after the handler executes. Think of them as middleware for your mediator requests.

**Common use cases:**
- Logging
- Validation
- Caching
- Performance monitoring
- Transaction management
- Authorization
- Error handling

## Example Behaviors in This Sample

### 1. LoggingBehavior
Logs every request/response with execution time.

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, ...)
    {
        Log("==> Handling request");
        var response = await next();  // Call next behavior or handler
        Log("<== Request completed");
        return response;
    }
}
```

### 2. ValidationBehavior
Validates requests before they reach the handler.

Requests implement `IValidatable`:
```csharp
public record CreateUserRequest(...) : IRequest<...>, IValidatable
{
    public List<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Username))
            errors.Add("Username is required");
        return errors;
    }
}
```

### 3. CachingBehavior
Caches responses for requests implementing `ICacheable`.

```csharp
public record GetUserRequest(Guid UserId) : IRequest<UserDto>, ICacheable
{
    public string GetCacheKey() => $"user_{UserId}";
    public int CacheDurationSeconds => 30;
}
```

### 4. PerformanceBehavior
Monitors execution time and logs warnings for slow requests (>500ms).

## How to Use Pipeline Behaviors

### Step 1: Create a Pipeline Behavior

```csharp
public class MyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Before handler execution
        Console.WriteLine("Before");

        var response = await next(); // Execute next behavior or handler

        // After handler execution
        Console.WriteLine("After");

        return response;
    }
}
```

### Step 2: Register the Behavior

```csharp
builder.Services.AddBellows(Assembly.GetExecutingAssembly());

// Register open generic type
builder.Services.AddPipelineBehavior(typeof(MyBehavior<,>));
```

**Important:** Behaviors execute in registration order!

```csharp
// Execution order: Logging -> Validation -> Caching -> Performance -> Handler
builder.Services.AddPipelineBehavior(typeof(LoggingBehavior<,>));
builder.Services.AddPipelineBehavior(typeof(ValidationBehavior<,>));
builder.Services.AddPipelineBehavior(typeof(CachingBehavior<,>));
builder.Services.AddPipelineBehavior(typeof(PerformanceBehavior<,>));
```

### Step 3: Requests Automatically Flow Through Behaviors

```csharp
// This request will flow through all registered behaviors
var response = await mediator.Send(new CreateUserRequest("john", "john@example.com", 25));
```

## Try the Demo Endpoints

### Valid User Creation
```bash
curl -X POST http://localhost:5000/users \
  -H "Content-Type: application/json" \
  -d '{"username":"john_doe","email":"john@example.com","age":25}'
```

### Invalid User (Validation Failure)
```bash
curl http://localhost:5000/demo/invalid-user
```

### Caching Demo
```bash
# First call: CACHE MISS
curl http://localhost:5000/users/12345678-1234-1234-1234-123456789012

# Second call: CACHE HIT (check logs!)
curl http://localhost:5000/users/12345678-1234-1234-1234-123456789012

# Or use the demo endpoint
curl http://localhost:5000/demo/caching
```

### Performance Monitoring
```bash
# Fast request (<500ms) - no warning
curl http://localhost:5000/reports/slow?durationMs=100

# Slow request (>500ms) - logs warning
curl http://localhost:5000/reports/slow?durationMs=1000
```

## Project Structure

```
AspNetCoreSample.PipelineBehavior/
├── Program.cs                          # Setup & endpoints
├── PipelineBehaviors/
│   ├── LoggingBehavior.cs             # Logs requests/responses
│   ├── ValidationBehavior.cs          # Validates requests
│   ├── CachingBehavior.cs             # Caches responses
│   └── PerformanceBehavior.cs         # Monitors performance
└── Features/
    └── UserManagement/
        ├── CreateUserRequest.cs       # Validated request
        ├── CreateUserHandler.cs
        ├── GetUserRequest.cs          # Cacheable request
        ├── GetUserHandler.cs
        ├── SlowReportRequest.cs       # Slow request
        └── SlowReportHandler.cs
```

## Watch the Logs!

When you run the sample, watch the console output to see behaviors in action:

```
==> Handling CreateUserRequest: {...}
Validating CreateUserRequest...
Validation passed for CreateUserRequest
Creating user: john_doe
<== Handled CreateUserRequest in 123ms: {...}
Performance: CreateUserRequest completed in 123ms
```

## Key Concepts

### Execution Order Matters
Behaviors execute like nested middleware:
```
LoggingBehavior
  ├─> ValidationBehavior
      ├─> CachingBehavior
          ├─> PerformanceBehavior
              ├─> Handler
              └─ (return)
          └─ (return)
      └─ (return)
  └─ (return)
```

### Conditional Behaviors
Use interfaces to make behaviors opt-in:
```csharp
if (request is ICacheable cacheable)
{
    // Only cache requests that implement ICacheable
}
```

### Short-Circuit the Pipeline
Don't call `next()` to stop execution:
```csharp
if (errors.Any())
{
    throw new ValidationException(errors); // Handler never executes
}
return await next(); // Continue to next behavior/handler
```

## Benefits

- **Separation of Concerns** - Keep cross-cutting logic separate from business logic
- **Reusability** - Write once, apply to all requests
- **Composability** - Mix and match behaviors as needed
- **Testability** - Test behaviors independently
- **Maintainability** - Easy to add/remove/modify behaviors
