namespace TheSwamp.WWW.Models;

/// <summary>
/// A resolved view of a <see cref="ChatMessage"/> returned to callers of
/// <see cref="Services.IChatService.GetRecentMessagesAsync"/>. Display name has already
/// been resolved from <c>UserId</c> by the service so consumers don't need UserManager.
/// </summary>
public record ChatMessageDto(
    long Id,
    string? UserId,
    string DisplayName,
    string Text,
    DateTime SentOnUtc);
