using System.Reflection;
using Bellows.Abstractions;
using Bellows.Extensions;
using AspNetCoreSample.PipelineBehavior.Features.UserManagement;
using AspNetCoreSample.PipelineBehavior.PipelineBehaviors;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add Bellows with handlers
builder.Services.AddBellows(Assembly.GetExecutingAssembly());

// Add pipeline behaviors (order matters - they execute in registration order)
builder.Services.AddPipelineBehavior(typeof(LoggingBehavior<,>));
builder.Services.AddPipelineBehavior(typeof(ValidationBehavior<,>));
builder.Services.AddPipelineBehavior(typeof(CachingBehavior<,>));
builder.Services.AddPipelineBehavior(typeof(PerformanceBehavior<,>));

var app = builder.Build();

// Root endpoint
app.MapGet("/", () => new
{
    Message = "Bellows Pipeline Behavior Sample",
    Endpoints = new
    {
        CreateUser = "POST /users",
        GetUser = "GET /users/{id}",
        SlowReport = "GET /reports/slow?durationMs={ms}",
        InvalidUser = "GET /demo/invalid-user"
    }
});

// Create user - demonstrates validation
app.MapPost("/users", async (CreateUserRequest request, IMediator mediator) =>
{
    try
    {
        var response = await mediator.Send(request);
        return Results.Created($"/users/{response.UserId}", response);
    }
    catch (ValidationException ex)
    {
        return Results.BadRequest(new { Errors = ex.Errors });
    }
});

// Get user - demonstrates caching
app.MapGet("/users/{id:guid}", async (Guid id, IMediator mediator) =>
{
    var request = new GetUserRequest(id);
    var user = await mediator.Send(request);
    return Results.Ok(user);
});

// Generate slow report - demonstrates performance monitoring
app.MapGet("/reports/slow", async (int durationMs, IMediator mediator) =>
{
    var request = new SlowReportRequest(durationMs);
    var report = await mediator.Send(request);
    return Results.Ok(report);
});

// Demo endpoint - shows validation failure
app.MapGet("/demo/invalid-user", async (IMediator mediator) =>
{
    try
    {
        var request = new CreateUserRequest("ab", "invalid-email", 15);
        await mediator.Send(request);
        return Results.Ok("Should not reach here");
    }
    catch (ValidationException ex)
    {
        return Results.BadRequest(new
        {
            Message = "Validation failed (as expected)",
            Errors = ex.Errors,
            Note = "Check the logs to see pipeline behaviors in action"
        });
    }
});

// Demo endpoint - shows caching in action
app.MapGet("/demo/caching", async (IMediator mediator) =>
{
    var userId = Guid.Parse("12345678-1234-1234-1234-123456789012");

    // First call - should miss cache
    await mediator.Send(new GetUserRequest(userId));

    // Second call - should hit cache
    await mediator.Send(new GetUserRequest(userId));

    return Results.Ok(new
    {
        Message = "Called GetUser twice with same ID",
        Note = "Check logs - second call should be a CACHE HIT",
        UserId = userId
    });
});

app.Run();