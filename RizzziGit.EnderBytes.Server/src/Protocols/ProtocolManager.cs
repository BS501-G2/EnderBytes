namespace RizzziGit.EnderBytes.Protocols;

using FileTransfer;

public sealed class ProtocolManager : Service
{
  public ProtocolManager(Server server) : base("Protocols", server)
  {
    Server = server;

    FTP = new(this);
  }

  public readonly Server Server;
  public readonly FileTransferProtocol FTP;

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await (await WatchDog([FTP], cancellationToken)).task;
  }

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await FTP.Start();
  }

  protected override async Task OnStop(Exception? exception)
  {
    await FTP.Stop();
  }
}
