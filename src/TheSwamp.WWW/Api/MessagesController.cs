using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TheSwamp.WWW.Models;
using TheSwamp.WWW.Services;

namespace TheSwamp.WWW.Api;

/// <summary>
/// API endpoints for chat messages.
/// Both endpoints are protected by the ApiKeyMiddleware (X-Api-Key header required).
/// All message logic is delegated to <see cref="IChatService"/> so the API and the
/// Blazor chat page share a single consistent code path.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly IChatService _chatService;

    public MessagesController(IChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>
    /// GET /api/messages
    /// Returns the most recent chat messages (up to 50), oldest first.
    /// Display names are resolved from user IDs by the service.
    /// </summary>
    [HttpGet]
    public async Task<IEnumerable<ChatMessageDto>> GetMessages()
    {
        return await _chatService.GetRecentMessagesAsync(50);
    }

    /// <summary>
    /// POST /api/messages
    /// Saves a new chat message and broadcasts it to all SignalR clients.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PostMessage([FromBody] PostMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest("Message text cannot be empty.");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var message = await _chatService.SendMessageAsync(userId, request.Text);
        return Ok(message);
    }
}

/// <summary>
/// Request body for POST /api/messages.
/// </summary>
public record PostMessageRequest(string Text);