using Theswamp.WWW.Models;

namespace Theswamp.WWW.Services;

/// <summary>
/// Centralises chat message logic so every send path (API, Blazor page, SignalR hub)
/// behaves consistently: message is saved to the database and then broadcast to all
/// connected SignalR clients.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Persists the message to the database and broadcasts it to all SignalR clients.
    /// </summary>
    /// <param name="userName">Display name of the sender. Defaults to "Anon" if empty.</param>
    /// <param name="text">Message text. Leading/trailing whitespace is trimmed.</param>
    /// <returns>The saved <see cref="ChatMessage"/> with its generated Id and timestamp.</returns>
    Task<ChatMessage> SendMessageAsync(string? userName, string text);

    /// <summary>
    /// Returns the most recent chat messages from the database, oldest first.
    /// </summary>
    /// <param name="count">Maximum number of messages to return.</param>
    Task<IReadOnlyList<ChatMessage>> GetRecentMessagesAsync(int count = 50);
}
