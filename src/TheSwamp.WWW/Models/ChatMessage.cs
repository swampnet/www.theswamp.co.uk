using System.ComponentModel.DataAnnotations;

namespace TheSwamp.WWW.Models;

/// <summary>
/// Represents a single chat message stored in the database.
/// </summary>
public class ChatMessage
{
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// Identity ID of the sender (FK to AspNetUsers.Id). Null for anonymous messages.
    /// Display name resolution is done at query time via UserManager — we never store
    /// a username here because it can change without the message table knowing.
    /// </summary>
    [MaxLength(450)] // matches AspNetUsers.Id column length
    public string? UserId { get; set; }

    /// <summary>The message text.</summary>
    [Required]
    [MaxLength(2000)]
    public string Text { get; set; } = string.Empty;

    /// <summary>UTC timestamp of when the message was sent.</summary>
    public DateTime SentOnUtc { get; set; } = DateTime.UtcNow;
}
