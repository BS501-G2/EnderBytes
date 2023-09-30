namespace RizzziGit.EnderBytes.Resources;

using Database;

public sealed partial class MainResourceManager : Shared.Resources.MainResourceManager
{
  public MainResourceManager(EnderBytesServer server)
  {
    Server = server;
    Logger = new("Resources");
    Users = new(this);

    Server.Logger.Subscribe(Logger);
  }

  ~MainResourceManager()
  {
    Server.Logger.Unsubscribe(Logger);
  }

  public readonly EnderBytesServer Server;
  public readonly EnderBytesLogger Logger;
  public Database? Database;

  public readonly UserResource.ResourceManager Users;

  public readonly string DatabaseDirectory = Path.Join(Environment.CurrentDirectory, ".db");
  public string DatabaseFile => Path.Join(DatabaseDirectory, "srv.db");

  public Database RequireDatabase()
  {
    Database database = Database ?? throw new InvalidOperationException("Database is not open.");

    return database;
  }

  private async Task Open(CancellationToken cancellationToken)
  {
    if (Database != null)
    {
      await Close();
    }

    if (!Directory.Exists(DatabaseDirectory))
    {
      Directory.CreateDirectory(DatabaseDirectory);
    }

    Database = await Database.Open(DatabaseFile, cancellationToken);
  }

  private async Task Close()
  {
    if (Database == null)
    {
      return;
    }

    await Database.Close();
    Database = null;
  }

  public override async Task Init(CancellationToken cancellationToken)
  {

    await Open(cancellationToken);
    await base.Init(cancellationToken);
  }
}
