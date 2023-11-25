namespace RizzziGit.EnderBytes.StoragePools;

using Connections;
using Resources.BlobStorage;

public sealed partial class BlobStoragePool
{
  private const string ROOT_NAME = ".ROOT";
  private const string TRASH_NAME = ".TRASH";

  private Root? Root;

  private sealed class BlobContext(BlobStoragePool pool, Connection connection) : Context(pool, connection)
  {
  }

  protected override Task<Handle> InternalGetByPath(Context context, Path path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override async Task<Root> InternalGetRoot(Context context, CancellationToken cancellationToken)
  {
    if (Root != null)
    {
      return Root;
    }

    FileNodeResource node = await Database.RunTransaction(
      (transaction) => Nodes.GetByName(transaction, ROOT_NAME, null) ?? Nodes.CreateFolder(transaction, ROOT_NAME, null),
      cancellationToken
    );

    return Root = new BlobRootHandle(this);
  }

  protected override IAsyncEnumerable<Handle> InternalGetTrashed(Context context, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}
