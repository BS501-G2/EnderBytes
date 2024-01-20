namespace RizzziGit.EnderBytes.Services.Resource;

using Resources;

public abstract class ResourceService : Server.SubService
{
  private ResourceService(Server server) : base(server, "Resources") { }

  public sealed class Main : ResourceService
  {
    public Main(Server server) : base(server)
    {
      Users = new(this);
      UserAuthentications = new(this);
    }

    public readonly User.ResourceManager Users;
    public readonly UserAuthentication.ResourceManager UserAuthentications;

    protected override Task OnStart(CancellationToken cancellationToken)
    {
      Users.Start(default);
      UserAuthentications.Start(default);

      return base.OnStart(cancellationToken);
    }

    protected override Task OnStop(Exception? exception)
    {
      Users.Stop();
      UserAuthentications.Stop();

      return base.OnStop(exception);
    }
  }
}
