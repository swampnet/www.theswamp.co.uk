using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace TheSwamp.WWW.Data;

public class ApplicationUser : IdentityUser
{
	/// <summary>
	/// Optional display name shown in chat and other UI contexts.
	/// When set, takes precedence over UserName and Email.
	/// </summary>
	[MaxLength(100)]
	public string? DisplayName { get; set; }

	/// <summary>
	/// SHA-256 hex hash of the user's API key.
	/// Null means no API key is currently active.
	/// The raw key is never stored — it is shown to the user once on generation.
	/// </summary>
	[MaxLength(64)]
	public string? ApiKeyHash { get; set; }
}

