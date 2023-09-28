namespace RizzziGit.EnderBytes;

using Buffer;
using RBMP;
using Resources;

public sealed partial class EnderBytesServer
{
  public EnderBytesServer()
  {
    ResourceManager = new(this);
  }

  private readonly MainResourceManager ResourceManager;

  public async Task Init(CancellationToken cancellationToken)
  {
    await ResourceManager.Init(cancellationToken);
  }
}
