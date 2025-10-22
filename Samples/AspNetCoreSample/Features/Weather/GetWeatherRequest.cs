using Bellows.Abstractions;

namespace AspNetCoreSample;

/// <summary>
/// Request to get weather forecast for a city
/// </summary>
public record GetWeatherRequest(string City) : IRequest<WeatherForecast>;

/// <summary>
/// Weather forecast response
/// </summary>
public record WeatherForecast(string City, int TemperatureC, string Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
