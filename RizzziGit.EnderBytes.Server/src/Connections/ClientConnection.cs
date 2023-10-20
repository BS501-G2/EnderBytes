namespace RizzziGit.EnderBytes.Connections;

public sealed class ClientConnection(ConnectionManager manager, ulong id) : Connection(manager, id)
{
}
