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
    Keys = new(this);
    FileHubs = new(this);
    Files = new(this);
    FileAccesses = new(this);
  }

  private SQLiteConnection? Connection;

  public readonly UserResource.ResourceManager Users;
  public readonly UserAuthenticationResource.ResourceManager UserAuthentications;
  public readonly UserConfigurationResource.ResourceManager UserConfiguration;
  public readonly KeyResource.ResourceManager Keys;
  public readonly FileHubResource.ResourceManager FileHubs;
  public readonly FileNodeResource.ResourceManager Files;
  public readonly FileAccessResource.ResourceManager FileAccesses;

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
    await Keys.Start(cancellationToken);
    await FileHubs.Start(cancellationToken);
    await Files.Start(cancellationToken);
    await FileAccesses.Start(cancellationToken);
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
          Keys,
          FileHubs,
          Files,
          FileAccesses
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
    await Users.Stop();
    await UserAuthentications.Stop();
    await UserConfiguration.Stop();
    await Keys.Stop();
    await FileHubs.Stop();
    await Files.Stop();
    await FileAccesses.Stop();

    TransactionQueueTaskCancellationTokenSource?.Cancel();
    Connection?.Dispose();
    Connection = null;
  }
}
