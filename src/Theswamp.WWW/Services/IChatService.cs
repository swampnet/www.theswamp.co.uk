using TheSwamp.WWW.Models;

namespace TheSwamp.WWW.Services;

/// <summary>
/// Centralises chat message logic so every send path (API, Blazor page, SignalR hub)
/// behaves consistently: message is saved to the database and then broadcast to all
/// connected SignalR clients.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Persists the message to the database and broadcasts it to all SignalR clients.
    /// The sender's display name is resolved from <paramref name="userId"/> at send time.
    /// </summary>
    /// <param name="userId">Identity user ID of the sender. Null for anonymous senders.</param>
    /// <param name="text">Message text. Leading/trailing whitespace is trimmed.</param>
    /// <returns>The saved <see cref="ChatMessage"/> with its generated Id and timestamp.</returns>
    Task<ChatMessage> SendMessageAsync(string? userId, string text);

    /// <summary>
    /// Returns the most recent chat messages from the database, oldest first.
    /// Display names are resolved from user IDs before returning.
    /// </summary>
    /// <param name="count">Maximum number of messages to return.</param>
    Task<IReadOnlyList<ChatMessageDto>> GetRecentMessagesAsync(int count = 50);

    /// <summary>
    /// Resolves a display name for the given user ID.
    /// Returns "Anon" for null or unknown IDs.
    /// </summary>
    Task<string> GetDisplayNameAsync(string? userId);
}
