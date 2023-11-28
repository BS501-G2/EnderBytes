using System.Runtime.CompilerServices;

namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Resources.BlobStorage;
using Database;
using Collections;

public sealed partial class BlobStoragePool : StoragePool
{
  private const string ROOT_NAME = ".ROOT";
  private const string TRASH_NAME = ".TRASH";

  public BlobStoragePool(StoragePoolManager manager, StoragePoolResource resource) : base(manager, resource)
  {
    Resources = new(this);
    Handles = new();
  }

  private readonly Resources.BlobStorage.ResourceManager Resources;
  private Database Database => Resources.Database;

  private FileNodeResource.ResourceManager Nodes => Resources.Nodes;

  private readonly DoublyWeakDictionary<FileNodeResource, Handle> Handles;

  private Handle ResolveHandle(FileNodeResource node)
  {
    if (!Handles.TryGetValue(node, out var value))
    {
      Handles.Add(node, value = node.Type switch
      {
        FileNodeType.File => new BlobFileHandle(this, node),
        FileNodeType.Directory => new BlobFolderHandle(this, node),
        FileNodeType.SymbolicLink => new BlobSymbolicLinkHandle(this, node),

        _ => throw new InvalidOperationException("Invalid node type.")
      });
    }

    return value;
  }

  protected override async Task<Root> InternalGetRoot(Context context, CancellationToken cancellationToken)
  {
    if (Root != null)
    {
      return Root;
    }

    return Root = new BlobRoot(
      this,
      await Database.RunTransaction(
        (transaction) => Nodes.GetByName(transaction, ROOT_NAME, null) ?? Nodes.CreateFolder(transaction, ROOT_NAME, null),
        cancellationToken
      ),
      await Database.RunTransaction(
        (transaction) => Nodes.GetByName(transaction, TRASH_NAME, null) ?? Nodes.CreateFolder(transaction, TRASH_NAME, null),
        cancellationToken
      )
    );
  }

  protected override async IAsyncEnumerable<TrashedHandle> InternalGetTrashed(Context context, [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    List<TrashedHandle> handles = [];

    BlobRoot root = (BlobRoot)await GetRoot(context, cancellationToken);
    BlobFolderHandle trashedFolder = (BlobFolderHandle)await root.GetRootFolder(context, cancellationToken);

    await Database.RunTransaction((transaction) =>
    {
      foreach (FileNodeResource node in Nodes.StreamChildrenNodes(transaction, trashedFolder.Resource))
      {
        if (node.TrashTime == null)
        {
          continue;
        }

        handles.Add(new(ResolveHandle(node), (long)node.TrashTime));
      }
    }, cancellationToken);

    foreach (TrashedHandle handle in handles)
    {
      yield return handle;
    }
  }

  protected override Task InternalStart(CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }

  protected override Task InternalRun(CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }

  protected override Task InternalStop(System.Exception? exception)
  {
    return Task.CompletedTask;
  }
}
