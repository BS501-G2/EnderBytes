namespace RizzziGit.EnderBytes.Services;

using Core;

public sealed class SessionService(Server server) : Server.SubService(server, "Sessions")
{
}
