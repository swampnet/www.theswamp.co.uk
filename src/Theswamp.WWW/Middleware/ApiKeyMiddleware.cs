namespace TheSwamp.WWW.Middleware;

/// <summary>
/// Simple API key authentication middleware.
/// Checks requests under /api/ for a valid X-Api-Key header.
/// The expected key is read from ApiSettings:ApiKey in configuration.
/// </summary>
public class ApiKeyMiddleware
{
	private const string API_KEY_HEADER = "X-Api-Key";
	private const string CONFIG_KEY = "ApiSettings:ApiKey";

	private readonly RequestDelegate _next;
	private readonly IConfiguration _configuration;

	public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
	{
		_next = next;
		_configuration = configuration;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		// Only protect /api/ routes.
		if (!context.Request.Path.StartsWithSegments("/api"))
		{
			await _next(context);
			return;
		}

		var expectedKey = _configuration[CONFIG_KEY];

		// If no key is configured, deny all API requests (fail-safe).
		if (string.IsNullOrWhiteSpace(expectedKey))
		{
			context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
			await context.Response.WriteAsync("API key not configured.");
			return;
		}

		// Check the request header.
		if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var providedKey)
			|| !string.Equals(providedKey, expectedKey, StringComparison.Ordinal))
		{
			context.Response.StatusCode = StatusCodes.Status401Unauthorized;
			await context.Response.WriteAsync("Unauthorized.");
			return;
		}

		await _next(context);
	}
}
