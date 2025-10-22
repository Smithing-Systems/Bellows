# Bellows ASP.NET Core Sample

A practical example demonstrating the **Bellows** mediator library in an ASP.NET Core application.

## Quick Start

### Installation

The Bellows package is already included in this project:

```xml
<PackageReference Include="Bellows" Version="9.0.0-alpha" />
```

### Running the Sample

```bash
# Navigate to the project directory
cd Samples/AspNetCoreSample

# Run the application
dotnet run

# The API will be available at http://localhost:5000
```

### Try It Out

```bash
# Get weather forecast
curl http://localhost:5000/weather/Warsaw

# Create an order
curl -X POST http://localhost:5000/orders \
  -H "Content-Type: application/json" \
  -d '{"productName":"Laptop","quantity":2,"price":999.99}'

# Create a sample order (easiest way to test)
curl http://localhost:5000/orders/sample
```

Watch the console logs to see multiple handlers executing in parallel when an order is created!

## What's Demonstrated

### Request/Response Pattern
Send a request and get a response through the mediator.

**Example:** Weather forecast endpoint uses `GetWeatherRequest` → `GetWeatherHandler`

### Notification Pattern (Pub/Sub)
Publish one notification, trigger multiple handlers automatically.

**Example:** Creating an order publishes `OrderCreatedNotification` which triggers:
- `EmailNotificationHandler` - Sends email notifications
- `InventoryNotificationHandler` - Updates inventory
- `AnalyticsNotificationHandler` - Records analytics

## Project Structure

```
AspNetCoreSample/
├── Program.cs                          # App setup & API endpoints
├── Features/
│   ├── Orders/
│   │   ├── CreateOrderRequest.cs      # Order request/response models
│   │   └── CreateOrderHandler.cs      # Handles order creation
│   └── Weather/
│       ├── GetWeatherRequest.cs       # Weather request/response models
│       └── GetWeatherHandler.cs       # Handles weather queries
└── Notifications/
    ├── OrderCreatedNotification.cs    # Order created event
    ├── EmailNotificationHandler.cs    # Sends emails
    ├── InventoryNotificationHandler.cs # Updates inventory
    └── AnalyticsNotificationHandler.cs # Records analytics
```

## Usage Guide

### 1. Setup Bellows in Program.cs

```csharp
builder.Services.AddBellows(Assembly.GetExecutingAssembly());
```

### 2. Inject IMediator in Your Endpoints

```csharp
app.MapGet("/weather/{city}", async (string city, IMediator mediator) =>
{
    var request = new GetWeatherRequest(city);
    var forecast = await mediator.Send(request);
    return Results.Ok(forecast);
});
```

### 3. Create Requests and Handlers

```csharp
// Request
public record GetWeatherRequest(string City) : IRequest<WeatherForecast>;

// Handler
public class GetWeatherHandler : IRequestHandler<GetWeatherRequest, WeatherForecast>
{
    public Task<WeatherForecast> Handle(GetWeatherRequest request, CancellationToken cancellationToken)
    {
        // Your logic here
    }
}
```

### 4. Use Notifications for Events

```csharp
// Notification
public record OrderCreatedNotification(Guid OrderId, string ProductName, decimal TotalPrice) : INotification;

// Multiple handlers can respond to one notification
public class EmailNotificationHandler : INotificationHandler<OrderCreatedNotification>
{
    public async Task Handle(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        // Send email
    }
}
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | Welcome message |
| GET | `/weather/{city}` | Get weather forecast for city |
| POST | `/orders` | Create new order |
| GET | `/orders/sample` | Create sample order (demo) |

## Benefits

- **Decoupling** - Endpoints don't know about handler implementations
- **Single Responsibility** - Each handler has one specific job
- **Extensibility** - Add new handlers without changing existing code
- **Testability** - Test handlers independently
