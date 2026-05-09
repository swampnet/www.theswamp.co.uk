using TheSwamp.WWW.Data;

namespace TheSwamp.WWW.Services;

/// <summary>
/// Manages per-user API keys: generation, revocation, and validation.
/// Keys are stored as SHA-256 hashes in <see cref="ApplicationUser.ApiKeyHash"/>.
/// The raw key is returned only at generation time and is never persisted.
/// </summary>
public interface IApiKeyService
{
	/// <summary>
	/// Generates a new API key for the specified user, stores its hash, and returns the raw key.
	/// Any previously active key is immediately invalidated (cache evicted + hash overwritten).
	/// </summary>
	/// <param name="userId">The Identity user ID.</param>
	/// <returns>The raw API key string. Show this to the user once — it cannot be retrieved later.</returns>
	Task<string> GenerateAsync(string userId);

	/// <summary>
	/// Revokes the API key for the specified user.
	/// The hash is cleared in the DB and evicted from the cache.
	/// </summary>
	/// <param name="userId">The Identity user ID.</param>
	Task RevokeAsync(string userId);

	/// <summary>
	/// Validates a raw API key supplied by a caller (e.g. from the X-Api-Key header).
	/// Checks the in-memory cache first; falls back to the database on a cache miss.
	/// </summary>
	/// <param name="rawKey">The raw key from the request header.</param>
	/// <returns>The owning <see cref="ApplicationUser"/> if the key is valid; otherwise null.</returns>
	Task<ApplicationUser?> ValidateAsync(string rawKey);
}
