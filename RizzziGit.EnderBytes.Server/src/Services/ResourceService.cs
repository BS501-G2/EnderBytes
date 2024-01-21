namespace RizzziGit.EnderBytes.Services;

using Core;
using Resources;

public sealed class ResourceService : Server.SubService
{
  public ResourceService(Server server) : base(server, "Resources")
  {
    Users = new(this);
    UserAuthentications = new(this);
    Storage = new(this);
  }

  public readonly User.ResourceManager Users;
  public readonly UserAuthentication.ResourceManager UserAuthentications;
  public readonly Storage.ResourceManager Storage;

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    await Users.Start();
    await UserAuthentications.Start();
    await Storage.Start();

    await base.OnStart(cancellationToken);
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await WatchDog([Users, UserAuthentications, Storage], cancellationToken);
    await base.OnRun(cancellationToken);
  }

  protected override async Task OnStop(Exception? exception)
  {
    await Storage.Stop();
    await UserAuthentications.Stop();
    await Users.Stop();

    await base.OnStop(exception);
  }
}
