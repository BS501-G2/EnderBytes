namespace RizzziGit.EnderBytes.Resources;

using Database;

public sealed class MainResourceManager
{
  public MainResourceManager(Server server)
  {
    Logger = new("Resources");
    Server = server;

    MainDatabase = new(Server, server.Config.DatabasePath, "Main");

    TableVersion = new(this, MainDatabase);
    Users = new(this, MainDatabase);
    UserAuthentications = new(this, MainDatabase);

    Server.Logger.Subscribe(Logger);
  }

  public readonly Logger Logger;
  public readonly Server Server;
  public readonly Database MainDatabase;
  public readonly TableVersionResource.ResourceManager TableVersion;
  public readonly UserResource.ResourceManager Users;
  public readonly UserAuthenticationResource.ResourceManager UserAuthentications;

  public async Task Run(TaskCompletionSource onReady, CancellationToken cancellationToken)
  {
    TaskCompletionSource onDatabaseReady = new();
    Task task = MainDatabase.RunDatabase(onDatabaseReady, cancellationToken);

    try
    {
      await onDatabaseReady.Task;
      await TableVersion.Init(cancellationToken);
      await Users.Init(cancellationToken);
      await UserAuthentications.Init(cancellationToken);

      onReady.SetResult();
    }
    catch (Exception exception)
    {
      onReady.SetException(exception);
    }
    await task;
  }
}
