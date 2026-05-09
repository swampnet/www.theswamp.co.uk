using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Theswamp.WWW.Models;

namespace Theswamp.WWW.Data;

/// <summary>
/// Main application database context.
/// Extends IdentityDbContext with full role support (IdentityRole).
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    /// <summary>Chat messages stored for the recent message history.</summary>
    public DbSet<ChatMessage> ChatMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Index on SentAt so we can efficiently query recent messages.
        builder.Entity<ChatMessage>()
            .ToTable("ChatMessage")
            .HasIndex(m => m.SentOnUtc);
    }
}
