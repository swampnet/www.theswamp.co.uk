using TheSwamp.WWW.Services;

namespace TheSwamp.WWW.Middleware;

/// <summary>
/// API key authentication middleware.
/// Checks requests under /api/ for a valid X-Api-Key header.
///
/// Keys are per-user and stored as SHA-256 hashes in the AspNetUsers table.
/// Validation is handled by <see cref="IApiKeyService"/>, which checks an in-memory
/// cache first and falls back to the database on a cache miss.
/// </summary>
public class ApiKeyMiddleware
{
	private const string ApiKeyHeader = "X-Api-Key";

	private readonly RequestDelegate _next;
	private readonly ILogger<ApiKeyMiddleware> _logger;

	public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger)
	{
		_next = next;
		_logger = logger;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		// Only protect /api/ routes.
		if (!context.Request.Path.StartsWithSegments("/api"))
		{
			await _next(context);
			return;
		}

		// Resolve IApiKeyService from the per-request service scope.
		// IApiKeyService is scoped (it needs ApplicationDbContext), so we cannot
		// inject it into the middleware constructor (which is effectively a singleton).
		var apiKeyService = context.RequestServices.GetRequiredService<IApiKeyService>();

		if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var rawKey)
			|| string.IsNullOrWhiteSpace(rawKey))
		{
			_logger.LogWarning(
				"API request rejected — missing {Header} header: {Method} {Path}",
				ApiKeyHeader, context.Request.Method, context.Request.Path);

			context.Response.StatusCode = StatusCodes.Status401Unauthorized;
			await context.Response.WriteAsync("Unauthorized.");
			return;
		}

		var user = await apiKeyService.ValidateAsync(rawKey!);
		if (user is null)
		{
			_logger.LogWarning(
				"API request rejected — invalid API key: {Method} {Path}",
				context.Request.Method, context.Request.Path);

			context.Response.StatusCode = StatusCodes.Status401Unauthorized;
			await context.Response.WriteAsync("Unauthorized.");
			return;
		}

		await _next(context);
	}
}

