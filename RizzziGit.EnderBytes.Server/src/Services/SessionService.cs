namespace RizzziGit.EnderBytes.Services;

using Core;

public sealed partial class SessionService(Server server, string name) : Server.SubService(server, name)
{
}
