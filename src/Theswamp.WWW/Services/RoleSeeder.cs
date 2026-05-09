using Microsoft.AspNetCore.Identity;

namespace Theswamp.WWW.Services;

/// <summary>
/// Seeds required roles into the database on startup.
/// Add any additional roles to the Roles array below.
///
/// To grant a user the "admin" role after they have registered,
/// update the AspNetUserRoles table directly:
///   INSERT INTO AspNetUserRoles (UserId, RoleId)
///   SELECT u.Id, r.Id
///   FROM AspNetUsers u, AspNetRoles r
///   WHERE u.Email = 'your@email.com' AND r.Name = 'admin'
///
/// Or use the Admin page once you have at least one admin user.
/// </summary>
public static class RoleSeeder
{
	/// <summary>All roles that must exist in the database.</summary>
	private static readonly string[] Roles = ["admin"];

	/// <summary>
	/// Ensures all required roles exist. Call this during app startup
	/// after the database has been migrated.
	/// </summary>
	public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
	{
		foreach (var roleName in Roles)
		{
			if (!await roleManager.RoleExistsAsync(roleName))
			{
				await roleManager.CreateAsync(new IdentityRole(roleName));
			}
		}
	}
}
