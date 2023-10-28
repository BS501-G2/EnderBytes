namespace RizzziGit.EnderBytes.Protocols;

using FileTransfer;
using SecureShell;

public sealed class ProtocolManager : Service
{
  public ProtocolManager(Server server) : base("Protocols", server)
  {
    Server = server;

    FTP = new(this);
    SSH = new(this);
  }

  public readonly Server Server;
  public readonly FileTransferProtocol FTP;
  public readonly SecureShellProtocol SSH;

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await (await WatchDog([FTP, SSH], cancellationToken)).task;
  }

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await FTP.Start();
    await SSH.Start();
  }

  protected override async Task OnStop(Exception? exception)
  {
    await FTP.Stop();
    await SSH.Stop();
  }
}
