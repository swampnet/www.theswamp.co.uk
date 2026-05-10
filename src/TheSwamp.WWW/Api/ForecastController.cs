using Microsoft.AspNetCore.Mvc;

namespace TheSwamp.WWW.Api;

/// <summary>
/// Example endpoint — returns a 5-day weather forecast.
/// Protected by API key middleware (X-Api-Key header required).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ForecastController : ControllerBase
{
	private static readonly string[] Summaries =
	[
		"Freezing", "Bracing", "Chilly", "Cool", "Mild",
		"Warm", "Balmy", "Hot", "Sweltering", "Scorching"
	];

	/// <summary>Returns a randomly generated 5-day weather forecast.</summary>
	[HttpGet]
	public IEnumerable<WeatherForecast> Get()
	{
		return Enumerable.Range(1, 5).Select(index => new WeatherForecast
		{
			Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
			TemperatureC = Random.Shared.Next(-20, 55),
			Summary = Summaries[Random.Shared.Next(Summaries.Length)]
		});
	}
}

/// <summary>Simple weather forecast DTO.</summary>
public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
	public WeatherForecast() : this(default, default, default) { }

	public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
