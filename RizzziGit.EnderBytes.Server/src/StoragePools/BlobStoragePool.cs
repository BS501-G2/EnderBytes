namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Buffer;
using Database;
using Connections;

public sealed class BlobStoragePool(StoragePoolManager manager, StoragePoolResource storagePool, FileStream blob) : StoragePool(manager, storagePool, StoragePoolType.Blob)
{
  private readonly FileStream Blob = blob;
  private readonly Database Database = manager.Server.Resources.MainDatabase;
  private readonly BlobFileResource.ResourceManager Files = manager.Server.Resources.Files;
  private readonly BlobFileSnapshotResource.ResourceManager Snapshots = manager.Server.Resources.FileSnapshots;
  private readonly BlobFileKeyResource.ResourceManager Keys = manager.Server.Resources.FileKeys;
  private readonly BlobFileDataResource.ResourceManager Data = manager.Server.Resources.FileData;

  private (BlobFileResource? parent, BlobFileResource? file) Find(DatabaseTransaction transaction, string[] path)
  {
    BlobFileResource? parent = null;
    BlobFileResource? file = null;
    foreach (string pathEntry in path)
    {
      parent = file;
      file = Files.GetByName(transaction, Resource, file, pathEntry);

      if (file == null)
      {
        break;
      }
    }

    return (parent, file);
  }

  // public override Task<StoragePoolResult> Execute(Connection connection, StoragePoolCommand command, CancellationToken cancellationToken) => command switch
  // {
  //   // StoragePoolCommand.ChangeOwner a => Handle(connection, a, cancellationToken),

  //   _ => Task.FromResult((StoragePoolResult)new StoragePoolResult.InvalidCommand())
  // };

  // private async Task<StoragePoolResult> Handle(Connection connection, StoragePoolCommand.ChangeOwner command, CancellationToken cancellationToken)
  // {
  //   var (path, user) = command;

  //   return await Database.RunTransaction<StoragePoolResult>(async (transaction, cancellationToken) =>
  //   {
  //     var (_, file) = Find(transaction, path);
  //     if (file == null)
  //     {
  //       return new StoragePoolResult.InvalidCommand();
  //     }

  //     await Files.UpdateOwner(transaction, file, user, cancellationToken);
  //     return new StoragePoolResult.OK();
  //   }, cancellationToken);
  // }
}
