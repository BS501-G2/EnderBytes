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
    FileBufferMaps = new(this);
    FileBuffers = new(this);
  }

  private Database? Database;

  public readonly UserResource.ResourceManager Users;
  public readonly UserAuthenticationResource.ResourceManager UserAuthentications;
  public readonly UserConfigurationResource.ResourceManager UserConfiguration;
  public readonly StorageResource.ResourceManager Storages;
  public readonly FileResource.ResourceManager Files;
  public readonly FileAccessResource.ResourceManager FileAccesses;
  public readonly FileSnapshotResource.ResourceManager FileSnapshots;
  public readonly FileBufferMapResource.ResourceManager FileBufferMaps;
  public readonly FileBufferResource.ResourceManager FileBuffers;

  private Task? TransactionQueueTask;
  private CancellationTokenSource? TransactionQueueTaskCancellationTokenSource = new();

  protected override async Task OnStart(CancellationToken cancellationToken)
  {
    Database = new MySQLDatabase(Server.Configuration.DatabaseConnectionStringBuilder);

    TransactionQueueTaskCancellationTokenSource = new();
    TransactionQueueTask = RunTransactionQueue(TransactionQueueTaskCancellationTokenSource.Token);

    await Users.Start(cancellationToken);
    await UserAuthentications.Start(cancellationToken);
    await UserConfiguration.Start(cancellationToken);
    await Storages.Start(cancellationToken);
    await Files.Start(cancellationToken);
    await FileAccesses.Start(cancellationToken);
    await FileSnapshots.Start(cancellationToken);
    await FileBufferMaps.Start(cancellationToken);
    await FileBuffers.Start(cancellationToken);
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
          FileBufferMaps,
          FileBuffers
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
    await FileBuffers.Stop();
    await FileBufferMaps.Stop();
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
