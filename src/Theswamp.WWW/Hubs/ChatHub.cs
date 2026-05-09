using Microsoft.AspNetCore.SignalR;
using TheSwamp.WWW.Services;

namespace TheSwamp.WWW.Hubs;

/// <summary>
/// SignalR hub for real-time chat.
/// Clients connect to /hubs/chat.
///
/// Client-side events emitted:
///   - "ReceiveMessage"    (userName: string, text: string, sentAt: string)
///   - "UserConnected"     (userName: string)
///   - "UserDisconnected"  (userName: string)
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
/// <param name="userName">Display name of the sender.</param>
/// <param name="text">Message text.</param>
public async Task SendMessage(string userName, string text)
{
// Service handles DB save + SignalR broadcast.
await _chatService.SendMessageAsync(userName, text);
}

/// <summary>Announces to all clients that a user has connected.</summary>
public override async Task OnConnectedAsync()
{
// The display name is passed as a query string parameter (?userName=...)
// so anonymous users can still connect without being authenticated.
var userName = Context.GetHttpContext()?.Request.Query["userName"].ToString();
if (string.IsNullOrWhiteSpace(userName))
{
userName = "Anon";
}

await Clients.All.SendAsync("UserConnected", userName);
await base.OnConnectedAsync();
}

/// <summary>Announces to all clients that a user has disconnected.</summary>
public override async Task OnDisconnectedAsync(Exception? exception)
{
var userName = Context.GetHttpContext()?.Request.Query["userName"].ToString();
if (string.IsNullOrWhiteSpace(userName))
{
userName = "Anon";
}

await Clients.Others.SendAsync("UserDisconnected", userName);
await base.OnDisconnectedAsync(exception);
}
}