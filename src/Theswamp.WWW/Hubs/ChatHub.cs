using Microsoft.AspNetCore.SignalR;
using TheSwamp.WWW.Services;

namespace TheSwamp.WWW.Hubs;

/// <summary>
/// SignalR hub for real-time chat.
/// Clients connect to /hubs/chat.
///
/// Client-side events emitted:
///   - "ReceiveMessage"    (displayName: string, text: string, sentAt: string)
///   - "UserConnected"     (displayName: string)
///   - "UserDisconnected"  (displayName: string)
///
/// For authenticated users, the display name is resolved server-side from the user ID.
/// Anonymous clients pass ?userId= (empty) in the query string and display as "Anon".
/// </summary>
public class ChatHub : Hub
{
    private readonly IChatService _chatService;

    public ChatHub(IChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>
    /// Invoked by a JS client to send a message.
    /// Routes through <see cref="IChatService"/> so the message is persisted to the
    /// database and broadcast to all clients via a single consistent code path.
    /// </summary>
    /// <param name="userId">Identity user ID of the sender. Null/empty for anonymous.</param>
    /// <param name="text">Message text.</param>
    public async Task SendMessage(string? userId, string text)
    {
        await _chatService.SendMessageAsync(userId, text);
    }

    /// <summary>Announces to all clients that a user has connected.</summary>
    public override async Task OnConnectedAsync()
    {
        // For authenticated users, Context.UserIdentifier is populated from the
        // NameIdentifier claim automatically by SignalR. For anonymous connections,
        // callers pass ?userId= in the query string (will be null/empty -> "Anon").
        var userId = Context.UserIdentifier
            ?? Context.GetHttpContext()?.Request.Query["userId"].ToString();

        var displayName = await _chatService.GetDisplayNameAsync(userId);
        await Clients.All.SendAsync("UserConnected", displayName);
        await base.OnConnectedAsync();
    }

    /// <summary>Announces to all clients that a user has disconnected.</summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier
            ?? Context.GetHttpContext()?.Request.Query["userId"].ToString();

        var displayName = await _chatService.GetDisplayNameAsync(userId);
        await Clients.Others.SendAsync("UserDisconnected", displayName);
        await base.OnDisconnectedAsync(exception);
    }
}