using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Theswamp.WWW.Data;
using Theswamp.WWW.Hubs;
using Theswamp.WWW.Models;

namespace Theswamp.WWW.Api;

/// <summary>
/// API endpoints for chat messages.
/// Both endpoints are protected by the ApiKeyMiddleware (X-Api-Key header required).
/// POST also broadcasts the message to all SignalR clients in real time.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
	private readonly ApplicationDbContext _db;
	private readonly IHubContext<ChatHub> _hub;

	// How many recent messages to return on GET.
	private const int RecentMessageCount = 50;

	public MessagesController(ApplicationDbContext db, IHubContext<ChatHub> hub)
	{
		_db = db;
		_hub = hub;
	}

	/// <summary>
	/// GET /api/messages
	/// Returns the most recent chat messages (up to 50), oldest first.
	/// </summary>
	[HttpGet]
	public async Task<IEnumerable<ChatMessage>> GetMessages()
	{
		return await _db.ChatMessages
			.OrderByDescending(m => m.SentAt)
			.Take(RecentMessageCount)
			.OrderBy(m => m.SentAt) // return oldest-first to the client
			.ToListAsync();
	}

	/// <summary>
	/// POST /api/messages
	/// Saves a new chat message to the database and broadcasts it
	/// to all connected SignalR clients via ChatHub.
	/// </summary>
	/// <param name="request">The message payload.</param>
	[HttpPost]
	public async Task<IActionResult> PostMessage([FromBody] PostMessageRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.Text))
		{
			return BadRequest("Message text cannot be empty.");
		}

		var message = new ChatMessage
		{
			UserName = string.IsNullOrWhiteSpace(request.UserName) ? "Anon" : request.UserName,
			Text = request.Text.Trim(),
			SentAt = DateTime.UtcNow
		};

		_db.ChatMessages.Add(message);
		await _db.SaveChangesAsync();

		// Broadcast to all connected SignalR clients so the chat page updates in real time.
		await _hub.Clients.All.SendAsync(
			"ReceiveMessage",
			message.UserName,
			message.Text,
			message.SentAt.ToString("o")); // ISO 8601

		return Ok(message);
	}
}

/// <summary>Request body for POST /api/messages.</summary>
public record PostMessageRequest(string? UserName, string Text);
