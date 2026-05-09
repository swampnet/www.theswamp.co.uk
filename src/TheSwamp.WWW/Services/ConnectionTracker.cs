using System.Collections.Concurrent;

namespace TheSwamp.WWW.Services;

/// <summary>
/// Represents a single active SignalR connection.
/// </summary>
public record ConnectedUser(
    string ConnectionId,
    string? UserId,
    string DisplayName,
    string IpAddress,
    DateTime ConnectedAt);

/// <summary>
/// Tracks all currently active SignalR hub connections.
/// Registered as a singleton so the registry is shared across all hub instances.
/// </summary>
public interface IConnectionTracker
{
    /// <summary>Records a new connection.</summary>
    void AddConnection(ConnectedUser user);

    /// <summary>Removes a connection by its connection ID.</summary>
    void RemoveConnection(string connectionId);

    /// <summary>Returns a snapshot of all currently connected users, newest first.</summary>
    IReadOnlyList<ConnectedUser> GetConnectedUsers();
}

/// <inheritdoc />
public class ConnectionTracker : IConnectionTracker
{
    // ConcurrentDictionary is safe for simultaneous hub connect/disconnect events
    // which can fire on any thread.
    private readonly ConcurrentDictionary<string, ConnectedUser> _connections = new();

    /// <inheritdoc />
    public void AddConnection(ConnectedUser user)
    {
        _connections[user.ConnectionId] = user;
    }

    /// <inheritdoc />
    public void RemoveConnection(string connectionId)
    {
        _connections.TryRemove(connectionId, out _);
    }

    /// <inheritdoc />
    public IReadOnlyList<ConnectedUser> GetConnectedUsers()
    {
        return _connections.Values
            .OrderByDescending(u => u.ConnectedAt)
            .ToList();
    }
}
