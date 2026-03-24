// SampleApi — Weather domain models

namespace SampleApi.Models;

/// <summary>
///     Represents current weather conditions at a location.
/// </summary>
/// <param name="Location">The location name (e.g., "San Francisco, CA").</param>
/// <param name="TemperatureC">Temperature in Celsius.</param>
/// <param name="TemperatureF">Temperature in Fahrenheit.</param>
/// <param name="Condition">The weather condition (e.g., "Sunny", "Cloudy").</param>
/// <param name="Humidity">Humidity percentage (0-100).</param>
/// <param name="WindSpeedKmh">Wind speed in km/h.</param>
/// <param name="ObservedAt">When this observation was recorded.</param>
public sealed record CurrentWeather(
    string Location,
    double TemperatureC,
    double TemperatureF,
    WeatherCondition Condition,
    int Humidity,
    double WindSpeedKmh,
    DateTime ObservedAt);

/// <summary>
///     Represents a single day's weather forecast.
/// </summary>
/// <param name="Date">The forecast date.</param>
/// <param name="HighC">Forecasted high temperature in Celsius.</param>
/// <param name="LowC">Forecasted low temperature in Celsius.</param>
/// <param name="Condition">Expected weather condition.</param>
/// <param name="PrecipitationChance">Chance of precipitation as a percentage (0-100).</param>
public sealed record WeatherForecast(
    DateOnly Date,
    double HighC,
    double LowC,
    WeatherCondition Condition,
    int PrecipitationChance);

/// <summary>
///     Describes the general weather condition.
/// </summary>
public enum WeatherCondition
{
    /// <summary>Clear skies with sunshine.</summary>
    Sunny,

    /// <summary>Partially cloudy.</summary>
    PartlyCloudy,

    /// <summary>Overcast skies.</summary>
    Cloudy,

    /// <summary>Light or heavy rain.</summary>
    Rainy,

    /// <summary>Thunderstorm with lightning.</summary>
    Stormy,

    /// <summary>Snow or sleet.</summary>
    Snowy,

    /// <summary>Fog or mist reducing visibility.</summary>
    Foggy
}