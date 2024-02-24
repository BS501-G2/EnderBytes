using System.Data.SQLite;
using System.Data.Common;
using MySql.Data.MySqlClient;

namespace RizzziGit.EnderBytes.Services;

using Core;
using Resources;
using DatabaseWrappers;

public sealed partial class ResourceService : Server.SubService
{
  public ResourceService(Server server) : base(server, "Resources")
  {
    Users = new(this);
    UserAuthentications = new(this);
    UserConfiguration = new(this);
    Storages = new(this);
    Files = new(this);
    FileAccesses = new(this);
    FileSnapshots = new(this);
    FileBuffers = new(this);
    FileBufferMaps = new(this);
  }

  private Database? Database;

  public readonly UserResource.ResourceManager Users;
  public readonly UserAuthenticationResource.ResourceManager UserAuthentications;
  public readonly UserConfigurationResource.ResourceManager UserConfiguration;
  public readonly StorageResource.ResourceManager Storages;
  public readonly FileResource.ResourceManager Files;
  public readonly FileAccessResource.ResourceManager FileAccesses;
  public readonly FileSnapshotResource.ResourceManager FileSnapshots;
  public readonly FileBufferResource.ResourceManager FileBuffers;
  public readonly FileBufferMapResource.ResourceManager FileBufferMaps;

  private Task? TransactionQueueTask;
  private CancellationTokenSource? TransactionQueueTaskCancellationTokenSource = new();

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    Database = Server.Configuration?.DatabaseConnectionStringBuilder switch
    {
      MySqlConnectionStringBuilder connectionStringBuilder => new MySQLDatabase(connectionStringBuilder),
      SQLiteConnectionStringBuilder connectionStringBuilder => new SQLiteDatabase(connectionStringBuilder),

      _ => new SQLiteDatabase(new SQLiteConnectionStringBuilder()
      {
        DataSource = Path.Join(Server.WorkingPath, $"Database.sqlite3"),
        JournalMode = SQLiteJournalModeEnum.Memory
      })
    };

    Logger.Info("Connecting to the database...");
    await Database.Connection.OpenAsync(cancellationToken);

    TransactionQueueTaskCancellationTokenSource = new();
    TransactionQueueTask = RunTransactionQueue(Database.Connection, TransactionQueueTaskCancellationTokenSource.Token);

    await Users.Start(cancellationToken);
    await UserAuthentications.Start(cancellationToken);
    await UserConfiguration.Start(cancellationToken);
    await Storages.Start(cancellationToken);
    await Files.Start(cancellationToken);
    await FileAccesses.Start(cancellationToken);
    await FileSnapshots.Start(cancellationToken);
    await FileBuffers.Start(cancellationToken);
    await FileBufferMaps.Start(cancellationToken);
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    try
    {
      await await Task.WhenAny([
        TransactionQueueTask!,
        WatchDog([
          Users,
          UserAuthentications,
          UserConfiguration,
          Storages,
          Files,
          FileAccesses,
          FileSnapshots,
          FileBuffers,
          FileBufferMaps
        ], cancellationToken)
      ]);
    }
    finally
    {
      TransactionQueueTask = null;
    }
  }

  protected override async Task OnStop(Exception? exception = null)
  {
    await FileBufferMaps.Stop();
    await FileBuffers.Stop();
    await FileSnapshots.Stop();
    await FileAccesses.Stop();
    await Files.Stop();
    await Storages.Stop();
    await UserConfiguration.Stop();
    await UserAuthentications.Stop();
    await Users.Stop();

    TransactionQueueTaskCancellationTokenSource?.Cancel();
    Database?.Dispose();
    Database = null;
  }
}
