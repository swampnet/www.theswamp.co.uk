using Microsoft.AspNetCore.Identity;
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
    private readonly UserManager<ApplicationUser> _userManager;

    public ChatService(
        ApplicationDbContext db,
        IHubContext<ChatHub> hub,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _hub = hub;
        _userManager = userManager;
    }

    /// <inheritdoc />
    public async Task<string> GetDisplayNameAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return "Anon";
        }

        var user = await _userManager.FindByIdAsync(userId);

        // Prefer UserName, fall back to Email, then "Anon" if the ID doesn't resolve.
        return user?.UserName ?? user?.Email ?? "Anon";
    }

    /// <inheritdoc />
    public async Task<ChatMessage> SendMessageAsync(string? userId, string text)
    {
        var displayName = await GetDisplayNameAsync(userId);

        var message = new ChatMessage
        {
            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId,
            Text = text.Trim(),
            SentOnUtc = DateTime.UtcNow
        };

        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();

        // Broadcast to all connected SignalR clients.
        // We send the resolved display name (not the raw userId) so clients need no lookup.
        // SentOnUtc is formatted as ISO 8601 so clients can parse it reliably.
        await _hub.Clients.All.SendAsync(
            "ReceiveMessage",
            displayName,
            message.Text,
            message.SentOnUtc.ToString("o"));

        return message;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatMessageDto>> GetRecentMessagesAsync(int count = 50)
    {
        var messages = await _db.ChatMessages
            .OrderByDescending(m => m.SentOnUtc)
            .Take(count)
            .OrderBy(m => m.SentOnUtc) // return oldest-first to the caller
            .ToListAsync();

        // Batch-resolve all unique non-null user IDs to avoid N+1 queries.
        var uniqueIds = messages
            .Where(m => m.UserId != null)
            .Select(m => m.UserId!)
            .Distinct()
            .ToList();

        var nameMap = new Dictionary<string, string>();
        foreach (var id in uniqueIds)
        {
            nameMap[id] = await GetDisplayNameAsync(id);
        }

        return messages.Select(m => new ChatMessageDto(
            m.Id,
            m.UserId,
            m.UserId != null && nameMap.TryGetValue(m.UserId, out var name) ? name : "Anon",
            m.Text,
            m.SentOnUtc
        )).ToList();
    }
}