namespace RizzziGit.EnderBytes.Services;

using Core;

public sealed partial class WebService(Server server) : Server.SubService(server, "Web")
{
}
