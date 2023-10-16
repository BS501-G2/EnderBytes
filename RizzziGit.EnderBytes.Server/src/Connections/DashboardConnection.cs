namespace RizzziGit.EnderBytes.Connections;

public sealed class DashboardConnection(ConnectionManager manager, ulong id, CancellationTokenSource cancellationTokenSource) : Connection(manager, id, cancellationTokenSource)
{
}
