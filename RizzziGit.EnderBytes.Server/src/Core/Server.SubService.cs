namespace RizzziGit.EnderBytes.Core;

using Commons.Services;

using Services;

public sealed partial class Server
{
    public abstract class SubService(Server server, string name) : Service(name, server)
    {
        public readonly Server Server = server;
    }
}
