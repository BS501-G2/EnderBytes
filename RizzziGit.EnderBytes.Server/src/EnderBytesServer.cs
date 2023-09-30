namespace RizzziGit.EnderBytes;

using Resources;

public struct EnderBytesConfig()
{
  public int DefaultPasswordIterations = 100000;
}

public sealed partial class EnderBytesServer
{
  public EnderBytesServer(EnderBytesConfig? config = null)
  {
    Logger = new("Server");
    Resources = new(this);
    Config = config ?? new();
  }

  private readonly MainResourceManager Resources;

  public readonly EnderBytesLogger Logger;
  public readonly EnderBytesConfig Config;

  public UserResource.ResourceManager Users => Resources.Users;

  public async Task Init(CancellationToken cancellationToken)
  {
    await Resources.Init(cancellationToken);
  }
}
