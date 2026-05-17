using System.Net.Http.Json;
using TheSwamp.PWA.Models;

namespace TheSwamp.PWA.Services;

public class AuthService
{
	private readonly HttpClient _http;
	private UserInfo? _cachedUser;
	private bool _fetched;

	public AuthService(HttpClient http)
	{
		_http = http;
	}

	/// <summary>
	/// Returns the logged-in user's info, or null if not authenticated.
	/// Result is cached for the lifetime of the service (one page load).
	/// </summary>
	public async Task<UserInfo?> GetUserAsync()
	{
		if (_fetched)
		{
			return _cachedUser;
		}

		_fetched = true;

		try
		{
			_cachedUser = await _http.GetFromJsonAsync<UserInfo>("pwa/me");
		}
		catch
		{
			// 401 or network error — treat as unauthenticated
			_cachedUser = null;
		}

		return _cachedUser;
	}

	/// <summary>
	/// Clears the cached user, forcing a re-fetch on the next call.
	/// </summary>
	public void Invalidate()
	{
		_fetched = false;
		_cachedUser = null;
	}
}
