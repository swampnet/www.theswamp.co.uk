using Microsoft.AspNetCore.SignalR;

namespace Theswamp.WWW.Hubs;

/// <summary>
/// SignalR hub for real-time chat.
/// Clients connect to /hubs/chat.
///
/// Client-side events emitted:
///   - "ReceiveMessage"  (userName: string, text: string, sentAt: string)
///   - "UserConnected"   (userName: string)
///   - "UserDisconnected" (userName: string)
/// </summary>
public class ChatHub : Hub
{
	/// <summary>
	/// Invoked by a client to broadcast a message to all connected clients.
	/// Also persists the message — see ChatController.PostMessage for API-driven persistence.
	/// </summary>
	/// <param name="userName">Display name of the sender.</param>
	/// <param name="text">Message text.</param>
	public async Task SendMessage(string userName, string text)
	{
		var sentAt = DateTime.UtcNow.ToString("o"); // ISO 8601
		await Clients.All.SendAsync("ReceiveMessage", userName, text, sentAt);
	}

	/// <summary>Announces to all clients that a user has connected.</summary>
	public override async Task OnConnectedAsync()
	{
		// The display name is passed as a query string parameter (?userName=...)
		// so anonymous users can still connect.
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
