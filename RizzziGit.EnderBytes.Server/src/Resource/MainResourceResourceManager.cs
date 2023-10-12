namespace RizzziGit.EnderBytes.Resources;

using Database;

public sealed class MainResourceManager : Shared.Resources.MainResourceManager
{
  public MainResourceManager(Server server)
  {
    Logger = new("Resources");
    Server = server;

    MainDatabase = new(Server, server.Config.DatabasePath, "Main");

    TableVersion = new(this, MainDatabase);
    Users = new(this, MainDatabase);

    Server.Logger.Subscribe(Logger);
  }

  public readonly Logger Logger;
  public readonly Server Server;
  public readonly Database MainDatabase;
  public readonly TableVersion.ResourceManager TableVersion;
  public readonly User.ResourceManager Users;

  public async Task Run(CancellationToken cancellationToken)
  {
    TaskCompletionSource onReady = new();
    Task task = MainDatabase.RunDatabase(onReady, cancellationToken);

    await onReady.Task;
    await TableVersion.Init(cancellationToken);
    await Users.Init(cancellationToken);
    await task;
  }
}
