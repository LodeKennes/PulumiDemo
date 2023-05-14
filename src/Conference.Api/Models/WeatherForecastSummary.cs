namespace Conference.Api.Models;

public record WeatherForecastSummary
{
    public int Id { get; init; }
    public string? Summary { get; init; }
}