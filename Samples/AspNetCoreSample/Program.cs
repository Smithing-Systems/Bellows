using System.Reflection;
using Bellows.Abstractions;
using Bellows.Extensions;
using AspNetCoreSample;

var builder = WebApplication.CreateBuilder(args);

// Add Bellows with handler registration
builder.Services.AddBellows(Assembly.GetExecutingAssembly());

var app = builder.Build();

// Root endpoint
app.MapGet("/", () => "Bellows Sample API - Try /weather/{city} or POST to /orders");

// Weather endpoint - demonstrates simple request/response
app.MapGet("/weather/{city}", async (string city, IMediator mediator) =>
{
    var request = new GetWeatherRequest(city);
    var forecast = await mediator.Send(request);
    return Results.Ok(forecast);
});

// Create order endpoint - demonstrates request/response with notifications
app.MapPost("/orders", async (CreateOrderRequest request, IMediator mediator) =>
{
    // Send request to create order
    var response = await mediator.Send(request);

    // Publish notification to multiple handlers
    var notification = new OrderCreatedNotification(
        response.OrderId,
        response.ProductName,
        response.TotalPrice
    );
    await mediator.Publish(notification);

    return Results.Created($"/orders/{response.OrderId}", response);
});

// Example endpoint showing inline request creation
app.MapGet("/orders/sample", async (IMediator mediator) =>
{
    // Create a sample order
    var request = new CreateOrderRequest("Sample Product", 2, 29.99m);
    var response = await mediator.Send(request);

    // Publish notification
    var notification = new OrderCreatedNotification(
        response.OrderId,
        response.ProductName,
        response.TotalPrice
    );
    await mediator.Publish(notification);

    return Results.Ok(new
    {
        Message = "Sample order created successfully",
        Order = response,
        Note = "Check the logs to see multiple notification handlers executing"
    });
});

app.Run();