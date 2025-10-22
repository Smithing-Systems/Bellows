using Bellows.Abstractions;

namespace AspNetCoreSample;

/// <summary>
/// Handles GetWeatherRequest by returning weather data
/// </summary>
public class GetWeatherHandler : IRequestHandler<GetWeatherRequest, WeatherForecast>
{
    private static readonly string[] Summaries =
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<GetWeatherHandler> _logger;

    public GetWeatherHandler(ILogger<GetWeatherHandler> logger)
    {
        _logger = logger;
    }

    public Task<WeatherForecast> Handle(GetWeatherRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting weather for {City}", request.City);

        var temperature = Random.Shared.Next(-20, 55);
        var summary = Summaries[Random.Shared.Next(Summaries.Length)];

        var forecast = new WeatherForecast(request.City, temperature, summary);

        return Task.FromResult(forecast);
    }
}
