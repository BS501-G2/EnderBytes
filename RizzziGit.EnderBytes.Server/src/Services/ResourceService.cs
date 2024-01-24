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
    WorkingPath = Path.Join(Server.WorkingPath, "Database");
    if (!File.Exists(WorkingPath))
    {
      Directory.CreateDirectory(WorkingPath);
    }
    Connections = [];
    Users = new(this);
  }

  private readonly Dictionary<Scope, SQLiteConnection> Connections;

  public readonly string WorkingPath;
  public readonly UserResource.ResourceManager Users;

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
  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    TransactionQueueTasks = new([.. Enum.GetValues<Scope>().Select((scope) => RunTransactionQueue(scope, cancellationToken))]);

    await Users.Start(cancellationToken);
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    try
    {
      await await Task.WhenAny([
        .. TransactionQueueTasks,
        WatchDog([Users], cancellationToken)
      ]);
    }
    finally
    {
      TransactionQueueTasks = new([]);
    }
  }

  protected override Task OnStop(Exception? exception = null)
  {
    foreach (Scope scope in Enum.GetValues<Scope>())
    {
      if (!Connections.TryGetValue(scope, out SQLiteConnection? connection))
      {
        continue;
      }

      connection.Dispose();
    }

    return Task.CompletedTask;
  }
}
