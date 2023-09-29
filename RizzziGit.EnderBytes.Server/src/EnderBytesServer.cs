namespace RizzziGit.EnderBytes;

using Resources;

public sealed partial class EnderBytesServer
{
  public EnderBytesServer()
  {
    Resources = new(this);
  }

  private readonly MainResourceManager Resources;

  public UserResource.ResourceManager Users => Resources.Users;

  public async Task Init(CancellationToken cancellationToken)
  {
    await Resources.Init(cancellationToken);
  }
}
