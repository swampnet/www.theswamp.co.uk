using System.Collections.Concurrent;

namespace TheSwamp.WWW.Services;

/// <summary>
/// In-memory cache for validated API keys.
/// Registered as a singleton so the cache is shared across all requests.
///
/// Keys are stored as SHA-256 hashes (not raw values) — the same format stored in the DB.
/// This means a cache hit requires only a hash comparison; the raw key is never retained
/// in memory after the middleware has finished hashing it.
///
/// Each cache entry also stores whether the owning user has the "api" role, so the
/// middleware can enforce role-based access without a DB round-trip on every request.
/// </summary>
public class ApiKeyCache
{
	/// <summary>Data stored per cached key hash.</summary>
	public record CacheEntry(string UserId, bool HasApiRole);

	// Primary map: SHA-256 hex hash → cache entry.
	private readonly ConcurrentDictionary<string, CacheEntry> _byHash = new(StringComparer.Ordinal);

	// Reverse map: userId → hash, used to evict a user's entry by ID (e.g. on role removal).
	private readonly ConcurrentDictionary<string, string> _byUserId = new(StringComparer.Ordinal);

	/// <summary>
	/// Attempts to resolve a cache entry from a key hash.
	/// Returns true and sets <paramref name="entry"/> if found.
	/// </summary>
	public bool TryGet(string keyHash, out CacheEntry? entry)
	{
		return _byHash.TryGetValue(keyHash, out entry);
	}

	/// <summary>
	/// Adds or updates a cache entry for the given key hash.
	/// Called after a successful DB validation so subsequent requests are served from memory.
	/// </summary>
	public void Set(string keyHash, string userId, bool hasApiRole)
	{
		// If this userId already has a cached hash (e.g. key was regenerated), remove the stale entry.
		if (_byUserId.TryGetValue(userId, out var oldHash) && oldHash != keyHash)
		{
			_byHash.TryRemove(oldHash, out _);
		}

		_byHash[keyHash] = new CacheEntry(userId, hasApiRole);
		_byUserId[userId] = keyHash;
	}

	/// <summary>
	/// Removes a cache entry by its key hash.
	/// Must be called whenever a key is revoked or regenerated so stale entries are not accepted.
	/// </summary>
	public void Remove(string keyHash)
	{
		if (_byHash.TryRemove(keyHash, out var entry))
		{
			_byUserId.TryRemove(entry.UserId, out _);
		}
	}

	/// <summary>
	/// Removes a cache entry by user ID.
	/// Call this when the "api" role is removed from a user so their cached key is
	/// immediately invalidated without waiting for the next cache miss.
	/// </summary>
	public void RemoveByUserId(string userId)
	{
		if (_byUserId.TryRemove(userId, out var hash))
		{
			_byHash.TryRemove(hash, out _);
		}
	}
}
