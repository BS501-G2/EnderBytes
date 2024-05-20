namespace RizzziGit.EnderBytes.Services;

using Core;

public sealed class ThumbnailService(Server server) : Server.SubService(server, "Thumbnails")
{
    protected override Task OnStart(CancellationToken cancellationToken)
    {
        return base.OnStart(cancellationToken);
    }
}
