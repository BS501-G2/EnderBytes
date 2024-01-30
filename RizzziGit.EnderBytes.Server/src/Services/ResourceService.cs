using System.Data.SQLite;
using System.Collections.ObjectModel;

namespace RizzziGit.EnderBytes.Services;

using Core;
using Resources;

public sealed partial class ResourceService : Server.SubService
{
  public enum Scope { Main, DataStorage }

  public ResourceService(Server server) : base(server, "Resources")
  {
    Connections = [];

    Users = new(this);
    UserAuthentications = new(this);
    UserConfiguration = new(this);
    Keys = new(this);
    FileHubs = new(this);
    Files = new(this);

    if (!File.Exists(WorkingPath))
    {
      Directory.CreateDirectory(WorkingPath);
    }
  }

  private readonly Dictionary<Scope, SQLiteConnection> Connections;

  public string WorkingPath => Path.Join(Server.WorkingPath, "Database");

  public readonly UserResource.ResourceManager Users;
  public readonly UserAuthenticationResource.ResourceManager UserAuthentications;
  public readonly UserConfigurationResource.ResourceManager UserConfiguration;
  public readonly KeyResource.ResourceManager Keys;
  public readonly FileHubResource.ResourceManager FileHubs;
  public readonly FileResource.ResourceManager Files;

  private SQLiteConnection GetDatabase(Scope scope)
  {
    lock (this)
    {
      if (!Connections.TryGetValue(scope, out SQLiteConnection? connection))
      {
        Connections.Add(scope, connection = new(new SQLiteConnectionStringBuilder()
        {
          DataSource = Path.Join(WorkingPath, $"{scope}.sqlite3"),
          JournalMode = SQLiteJournalModeEnum.Memory
        }.ConnectionString));

        connection.Open();
      }

      return connection;
    }
  }

  private ReadOnlyCollection<Task> TransactionQueueTasks = new([]);
  private CancellationTokenSource? TransactionQueueTaskCancellationTokenSource = new();

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    TransactionQueueTaskCancellationTokenSource = new();
    TransactionQueueTasks = new([.. Enum.GetValues<Scope>().Select((scope) => RunTransactionQueue(scope, TransactionQueueTaskCancellationTokenSource.Token))]);

    await Users.Start(cancellationToken);
    await UserAuthentications.Start(cancellationToken);
    await UserConfiguration.Start(cancellationToken);
    await Keys.Start(cancellationToken);
    await FileHubs.Start(cancellationToken);
    await Files.Start(cancellationToken);
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    try
    {
      await await Task.WhenAny([
        .. TransactionQueueTasks,

        WatchDog([Users, UserAuthentications, UserConfiguration, Keys, FileHubs, Files], cancellationToken)
      ]);
    }
    finally
    {
      TransactionQueueTasks = new([]);
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

    TransactionQueueTaskCancellationTokenSource?.Cancel();

    foreach (Scope scope in Enum.GetValues<Scope>())
    {
      if (!Connections.TryGetValue(scope, out SQLiteConnection? connection))
      {
        continue;
      }

      connection.Dispose();
    }
  }
}
