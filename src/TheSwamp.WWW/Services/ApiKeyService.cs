using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TheSwamp.WWW.Data;

namespace TheSwamp.WWW.Services;

/// <inheritdoc />
public class ApiKeyService : IApiKeyService
{
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly ApiKeyCache _cache;

	public ApiKeyService(UserManager<ApplicationUser> userManager, ApiKeyCache cache)
	{
		_userManager = userManager;
		_cache = cache;
	}

	/// <inheritdoc />
	public async Task<string> GenerateAsync(string userId)
	{
		var user = await _userManager.FindByIdAsync(userId)
			?? throw new InvalidOperationException($"User '{userId}' not found.");

		// Evict the old key from cache before overwriting so there is no window
		// where both the old and new key are simultaneously valid.
		if (!string.IsNullOrEmpty(user.ApiKeyHash))
		{
			_cache.Remove(user.ApiKeyHash);
		}

		// Generate a cryptographically random 32-byte key encoded as Base64Url (~43 chars).
		// Base64Url avoids '+', '/', '=' characters that can cause issues in HTTP headers.
		var rawKeyBytes = RandomNumberGenerator.GetBytes(32);
		var rawKey = Base64UrlEncode(rawKeyBytes);

		user.ApiKeyHash = HashKey(rawKey);

		var result = await _userManager.UpdateAsync(user);
		if (!result.Succeeded)
		{
			throw new InvalidOperationException(
				$"Failed to save API key: {string.Join("; ", result.Errors.Select(e => e.Description))}");
		}

		// Do NOT pre-populate the cache here — the first authenticated request will do it.
		// This avoids the cache and DB drifting if the update somehow fails mid-flight.

		return rawKey;
	}

	/// <inheritdoc />
	public async Task RevokeAsync(string userId)
	{
		var user = await _userManager.FindByIdAsync(userId)
			?? throw new InvalidOperationException($"User '{userId}' not found.");

		if (!string.IsNullOrEmpty(user.ApiKeyHash))
		{
			_cache.Remove(user.ApiKeyHash);
		}

		user.ApiKeyHash = null;
		await _userManager.UpdateAsync(user);
	}

	/// <inheritdoc />
	public async Task<ApplicationUser?> ValidateAsync(string rawKey)
	{
		if (string.IsNullOrWhiteSpace(rawKey))
		{
			return null;
		}

		var hash = HashKey(rawKey);

		// Fast path: cache hit avoids a DB round-trip.
		if (_cache.TryGet(hash, out var cachedUserId))
		{
			// Return the full user object so callers have identity info if needed.
			return await _userManager.FindByIdAsync(cachedUserId);
		}

		// Cache miss: query the DB. Only one user should ever have this hash (unique key).
		// UserManager doesn't expose a direct query by custom property, so we use the
		// underlying IQueryable from Users.
		var user = await _userManager.Users
			.FirstOrDefaultAsync(u => u.ApiKeyHash == hash);

		if (user is not null)
		{
			// Populate the cache so future requests are served without a DB hit.
			_cache.Set(hash, user.Id);
		}

		return user;
	}

	/// <summary>
	/// Computes a SHA-256 hex string of the given raw key.
	/// SHA-256 is appropriate here — API keys are high-entropy random strings,
	/// so a fast hash is safe (unlike passwords which need bcrypt/Argon2).
	/// </summary>
	private static string HashKey(string rawKey)
	{
		var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
		return Convert.ToHexString(bytes).ToLowerInvariant(); // 64-char lowercase hex
	}

	/// <summary>
	/// Encodes bytes as a URL-safe Base64 string without padding characters.
	/// </summary>
	private static string Base64UrlEncode(byte[] bytes)
	{
		return Convert.ToBase64String(bytes)
			.Replace('+', '-')
			.Replace('/', '_')
			.TrimEnd('=');
	}
}
