namespace RizzziGit.EnderBytes.Connections;

public sealed class ClientConnection(ConnectionManager manager, ulong id, CancellationTokenSource cancellationTokenSource) : Connection(manager, id, cancellationTokenSource)
{
}
