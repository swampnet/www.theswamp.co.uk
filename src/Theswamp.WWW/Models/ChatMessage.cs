using System.ComponentModel.DataAnnotations;

namespace TheSwamp.WWW.Models;

/// <summary>
/// Represents a single chat message stored in the database.
/// </summary>
public class ChatMessage
{
    [Key]
    public long Id { get; set; }

    /// <summary>Display name of the sender. "Anon" for unauthenticated users.</summary>
    [Required]
    [MaxLength(100)]
    public string UserName { get; set; } = "Anon";

    /// <summary>The message text.</summary>
    [Required]
    [MaxLength(2000)]
    public string Text { get; set; } = string.Empty;

    /// <summary>UTC timestamp of when the message was sent.</summary>
    //public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime SentOnUtc { get; set; } = DateTime.UtcNow;
}
