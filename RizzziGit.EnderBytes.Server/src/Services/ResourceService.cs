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
    Keys = new(this);

    if (!File.Exists(WorkingPath))
    {
      Directory.CreateDirectory(WorkingPath);
    }
  }

  private readonly Dictionary<Scope, SQLiteConnection> Connections;

  public string WorkingPath => Path.Join(Server.WorkingPath, "Database");

  public readonly UserResource.ResourceManager Users;
  public readonly UserAuthenticationResource.ResourceManager UserAuthentications;
  public readonly KeyResource.ResourceManager Keys;

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
    await Keys.Start(cancellationToken);
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    try
    {
      await await Task.WhenAny([
        .. TransactionQueueTasks,

        WatchDog([Users, UserAuthentications, Keys], cancellationToken)
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
    await Keys.Stop();

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
