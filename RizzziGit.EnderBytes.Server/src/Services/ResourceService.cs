namespace RizzziGit.EnderBytes.Services;

using Core;
using Resources;

public sealed class ResourceService : Server.SubService
{
  public ResourceService(Server server) : base(server, "Resources")
  {
    Users = new(this);
    UserAuthentications = new(this);
  }

  public readonly User.ResourceManager Users;
  public readonly UserAuthentication.ResourceManager UserAuthentications;

  protected override Task OnStart(CancellationToken cancellationToken)
  {
    return base.OnStart(cancellationToken);
  }

  protected override Task OnRun(CancellationToken cancellationToken)
  {
    return base.OnRun(cancellationToken);
  }

  protected override Task OnStop(Exception? exception)
  {
    return base.OnStop(exception);
  }
}
