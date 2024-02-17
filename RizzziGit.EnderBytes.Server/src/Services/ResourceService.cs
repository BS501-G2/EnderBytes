using System.Data.SQLite;

namespace RizzziGit.EnderBytes.Services;

using Core;
using Resources;

public sealed partial class ResourceService : Server.SubService
{
  public ResourceService(Server server) : base(server, "Resources")
  {
    Users = new(this);
    UserAuthentications = new(this);
    UserConfiguration = new(this);
    Storages = new(this);
    Files = new(this);
  }

  private SQLiteConnection? Connection;

  public readonly UserResource.ResourceManager Users;
  public readonly UserAuthenticationResource.ResourceManager UserAuthentications;
  public readonly UserConfigurationResource.ResourceManager UserConfiguration;
  public readonly StorageResource.ResourceManager Storages;
  public readonly FileResource.ResourceManager Files;

  private Task? TransactionQueueTask;
  private CancellationTokenSource? TransactionQueueTaskCancellationTokenSource = new();

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    Connection = new(new SQLiteConnectionStringBuilder()
    {
      DataSource = Path.Join(Server.WorkingPath, $"Database.sqlite3"),
      JournalMode = SQLiteJournalModeEnum.Memory
    }.ConnectionString);

    await Connection.OpenAsync(cancellationToken);

    TransactionQueueTaskCancellationTokenSource = new();
    TransactionQueueTask = RunTransactionQueue(Connection, TransactionQueueTaskCancellationTokenSource.Token);

    await Users.Start(cancellationToken);
    await UserAuthentications.Start(cancellationToken);
    await UserConfiguration.Start(cancellationToken);
    await Storages.Start(cancellationToken);
    await Files.Start(cancellationToken);
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
          Files
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
    await Files.Stop();
    await Storages.Stop();
    await UserConfiguration.Stop();
    await UserAuthentications.Stop();
    await Users.Stop();

    TransactionQueueTaskCancellationTokenSource?.Cancel();
    Connection?.Dispose();
    Connection = null;
  }
}
