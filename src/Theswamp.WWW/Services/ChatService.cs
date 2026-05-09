using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TheSwamp.WWW.Data;
using TheSwamp.WWW.Hubs;
using TheSwamp.WWW.Models;

namespace TheSwamp.WWW.Services;

/// <summary>
/// Default implementation of <see cref="IChatService"/>.
/// Registered as Scoped to match the lifetime of <see cref="ApplicationDbContext"/>.
/// <see cref="IHubContext{ChatHub}"/> is singleton-safe and can be used from any lifetime.
/// </summary>
public class ChatService : IChatService
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<ChatHub> _hub;

    public ChatService(ApplicationDbContext db, IHubContext<ChatHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    /// <inheritdoc />
    public async Task<ChatMessage> SendMessageAsync(string? userName, string text)
    {
        var message = new ChatMessage
        {
            UserName = string.IsNullOrWhiteSpace(userName) ? "Anon" : userName.Trim(),
            Text = text.Trim(),
            SentOnUtc = DateTime.UtcNow
        };

        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();

        // Broadcast to all connected SignalR clients.
        // SentOnUtc is formatted as ISO 8601 so clients can parse it reliably.
        await _hub.Clients.All.SendAsync(
        "ReceiveMessage",
        message.UserName,
        message.Text,
        message.SentOnUtc.ToString("o"));

        return message;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatMessage>> GetRecentMessagesAsync(int count = 50)
    {
        return await _db.ChatMessages
        .OrderByDescending(m => m.SentOnUtc)
        .Take(count)
        .OrderBy(m => m.SentOnUtc) // return oldest-first to the caller
        .ToListAsync();
    }
}