using System.Collections.Concurrent;

namespace TheSwamp.WWW.Services;

/// <summary>
/// In-memory cache for validated API keys.
/// Registered as a singleton so the cache is shared across all requests.
///
/// Keys are stored as SHA-256 hashes (not raw values) — the same format stored in the DB.
/// This means a cache hit requires only a hash comparison; the raw key is never retained
/// in memory after the middleware has finished hashing it.
/// </summary>
public class ApiKeyCache
{
	// Maps SHA-256 hex hash → the owner's Identity user ID.
	private readonly ConcurrentDictionary<string, string> _entries = new(StringComparer.Ordinal);

	/// <summary>
	/// Attempts to resolve a user ID from a cached key hash.
	/// Returns true and sets <paramref name="userId"/> if found.
	/// </summary>
	public bool TryGet(string keyHash, out string userId)
	{
		return _entries.TryGetValue(keyHash, out userId!);
	}

	/// <summary>
	/// Adds or updates a cache entry mapping <paramref name="keyHash"/> → <paramref name="userId"/>.
	/// Called after a successful DB validation so subsequent requests are served from memory.
	/// </summary>
	public void Set(string keyHash, string userId)
	{
		_entries[keyHash] = userId;
	}

	/// <summary>
	/// Removes a cache entry by its key hash.
	/// Must be called whenever a key is revoked or regenerated so stale entries are not accepted.
	/// </summary>
	public void Remove(string keyHash)
	{
		_entries.TryRemove(keyHash, out _);
	}
}
