namespace RizzziGit.EnderBytes.Services;

using Core;
using Protocols;

public sealed partial class ProtocolService : Server.SubService
{
  public ProtocolService(Server server) : base(server, "Protocols")
  {
    FtpProtocol = new(this);
  }

  public readonly FtpProtocol FtpProtocol;

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await FtpProtocol.Start(cancellationToken);
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await WatchDog([FtpProtocol], cancellationToken);
  }

  protected override async Task OnStop(Exception? exception = null)
  {
    await FtpProtocol.Stop();
  }
}
