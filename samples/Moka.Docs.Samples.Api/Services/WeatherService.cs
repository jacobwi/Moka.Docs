// SampleApi — Mock weather service

using SampleApi.Models;

namespace SampleApi.Services;

/// <summary>
///     Mock weather service that generates random weather data.
///     In a real application, this would call an external weather API.
/// </summary>
public sealed class WeatherService
{
	private static readonly string[] Locations = ["San Francisco, CA", "New York, NY", "London, UK"];
	private static readonly WeatherCondition[] Conditions = Enum.GetValues<WeatherCondition>();

	/// <summary>
	///     Gets the current weather conditions for the default location.
	/// </summary>
	/// <returns>Current weather data.</returns>
	public CurrentWeather GetCurrent()
	{
		int tempC = Random.Shared.Next(-5, 35);
		return new CurrentWeather(
			Locations[Random.Shared.Next(Locations.Length)],
			tempC,
			32 + tempC * 9.0 / 5.0,
			Conditions[Random.Shared.Next(Conditions.Length)],
			Random.Shared.Next(20, 95),
			Math.Round(Random.Shared.NextDouble() * 50, 1),
			DateTime.UtcNow);
	}

	/// <summary>
	///     Gets a weather forecast for the specified number of days.
	/// </summary>
	/// <param name="days">Number of days to forecast (1-14). Defaults to 5.</param>
	/// <returns>A list of daily forecasts.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="days" /> is not between 1 and 14.</exception>
	public List<WeatherForecast> GetForecast(int days = 5)
	{
		if (days is < 1 or > 14)
		{
			throw new ArgumentOutOfRangeException(nameof(days), "Days must be between 1 and 14.");
		}

		return Enumerable.Range(1, days).Select(i =>
		{
			int high = Random.Shared.Next(10, 35);
			return new WeatherForecast(
				DateOnly.FromDateTime(DateTime.Today.AddDays(i)),
				high,
				high - Random.Shared.Next(5, 15),
				Conditions[Random.Shared.Next(Conditions.Length)],
				Random.Shared.Next(0, 100));
		}).ToList();
	}
}
