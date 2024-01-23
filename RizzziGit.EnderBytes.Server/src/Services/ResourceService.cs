using Microsoft.Data.Sqlite;

namespace RizzziGit.EnderBytes.Services;

using System.Collections.ObjectModel;
using System.Threading;
using System.Transactions;
using Core;
using RizzziGit.EnderBytes.Resources;

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

  private readonly Dictionary<Scope, SqliteConnection> Connections;

  public readonly string WorkingPath;
  public readonly UserResource.ResourceManager Users;

  private SqliteConnection GetDatabase(Scope scope)
  {
    lock (this)
    {
      if (!Connections.TryGetValue(scope, out SqliteConnection? connection))
      {
        Connections.Add(scope, connection = new(new SqliteConnectionStringBuilder()
        {
          DataSource = Path.Join(WorkingPath, $"{scope}.sqlite3")
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
      if (!Connections.TryGetValue(scope, out SqliteConnection? connection))
      {
        continue;
      }

      connection.Dispose();
    }

    return Task.CompletedTask;
  }
}
