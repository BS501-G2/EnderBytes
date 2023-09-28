namespace RizzziGit.EnderBytes.Resources;

using Database;

public sealed partial class MainResourceManager(EnderBytesServer server) : Shared.Resources.MainResourceManager
{
  public readonly EnderBytesServer Server = server;
  public Database? Database;

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
