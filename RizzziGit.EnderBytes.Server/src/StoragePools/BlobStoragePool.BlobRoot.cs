using System.Runtime.CompilerServices;

namespace RizzziGit.EnderBytes.StoragePools;

using Resources.BlobStorage;

public sealed partial class BlobStoragePool
{
  private sealed class BlobRoot : Root
  {
    public BlobRoot(BlobStoragePool storagePool, FileNodeResource folderNode, FileNodeResource trashNode) : base(storagePool)
    {
      Pool = storagePool;

      FolderNode = folderNode;
      TrashNode = trashNode;

      RootFolder = new(storagePool, FolderNode);
      TrashFolder = new(storagePool, TrashNode);
    }

    private readonly new BlobStoragePool Pool;

    private readonly FileNodeResource FolderNode;
    private readonly FileNodeResource TrashNode;

    private readonly BlobFolderHandle RootFolder;
    private readonly BlobFolderHandle TrashFolder;

    protected override Task<Handle> InternalGetByPath(Context context, Path path, CancellationToken cancellationToken) => Pool.Database.RunTransaction((transaction) =>
    {
      FileNodeResource? node = FolderNode;
      foreach (string pathEntry in path)
      {
        node = Pool.Nodes.GetByName(transaction, pathEntry, node);

        if (node == null)
        {
          break;
        }
      }

      if (node == null)
      {
        throw new Exception.HandleNotFound();
      }

      return Pool.ResolveHandle(node);
    }, cancellationToken);

    protected override Task<Handle.Folder> InternalGetRootFolder(Context context, CancellationToken cancellationToken)
    {
      return Task.FromResult<Handle.Folder>(RootFolder);
    }

    protected override async IAsyncEnumerable<Handle> InternalListTrashed(Context context, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
      List<Handle> handles = [];

      await Pool.Database.RunTransaction((transaction) =>
      {
        foreach (FileNodeResource node in Pool.Nodes.StreamChildrenNodes(transaction, TrashNode))
        {
          handles.Add(Pool.ResolveHandle(node));
        }
      }, cancellationToken);

      foreach (Handle handle in handles)
      {
        yield return handle;
      }
    }
  }
}
