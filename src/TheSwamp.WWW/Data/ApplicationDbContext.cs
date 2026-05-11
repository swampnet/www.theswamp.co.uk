using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TheSwamp.WWW.Models;

namespace TheSwamp.WWW.Data;

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

        // PhoneNumber and PhoneNumberConfirmed are never captured — exclude from schema.
        builder.Entity<ApplicationUser>()
            .Ignore(u => u.PhoneNumber)
            .Ignore(u => u.PhoneNumberConfirmed);

        // Index on SentOnUtc so we can efficiently query recent messages.
        builder.Entity<ChatMessage>()
            .ToTable("ChatMessage")
            .HasIndex(m => m.SentOnUtc);
    }


    public async Task<IReadOnlyCollection<WineDto>> SearchWineAsync(string term)
    {
        var xx = await Database.SqlQuery<WineDto>($"exec [lwin].[Search] @term = {term}").ToArrayAsync();

        return xx;
    }
}
