namespace TheSwamp.PWA.Models;

public class UserInfo
{
	public string DisplayName { get; set; } = string.Empty;
	public IReadOnlyCollection<string> Roles { get; set; } = [];
}
