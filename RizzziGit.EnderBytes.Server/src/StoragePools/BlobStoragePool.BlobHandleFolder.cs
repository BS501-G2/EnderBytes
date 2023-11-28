namespace RizzziGit.EnderBytes.StoragePools;

using System.Security.Cryptography;
using Resources.BlobStorage;

public sealed partial class BlobStoragePool
{
  private sealed class BlobFolderHandle(BlobStoragePool pool, FileNodeResource resource) : Handle.Folder(pool)
  {
    private new readonly BlobStoragePool Pool = pool;

    public readonly FileNodeResource Resource = resource;

    public override long Id => Resource.Id;

    protected override async Task<long?> InternalGetTrashTime(Context context, CancellationToken cancellationToken)
    {
      await Pool.Database.RunTransaction((transaction) =>
      {
        if (Resource.ParentId != null)
        {
          FileNodeResource? parent = Pool.Nodes.GetById(transaction, (long)Resource.ParentId);
        }
      }, cancellationToken);
    }

    protected override Task<long?> InternalGetAccessTime(Context context, CancellationToken cancellationToken) => Task.FromResult(Resource.AccessTime);
    protected override Task<string> InternalGetName(Context context, CancellationToken cancellationToken) => Task.FromResult(Resource.Name);

    protected override async Task<File> InternalCreateFile(Context context, string name, long preallocateLength, CancellationToken cancellationToken)
    {
      await Pool.Database.RunTransaction((transaction) =>
      {
        // FileNodeResource node = Pool.Nodes.CreateFile(transaction, name, Resource);
      }, cancellationToken);
    }

    protected override async Task<Folder> InternalCreateFolder(Context context, string name, CancellationToken cancellationToken)
    {
      return await Pool.Database.RunTransaction((transaction) =>
      {
        if (Pool.Nodes.GetByName(transaction, name, Resource) != null)
        {
          throw new Exception.HandleExists();
        }

        return (BlobFolderHandle)Pool.ResolveHandle(Pool.Nodes.CreateFolder(transaction, name, Resource));
      }, cancellationToken);
    }

    protected override Task<SymbolicLink> InternalCreateSymbolicLink(Context context, string name, Path target, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override async Task<Folder?> InternalGetParent(Context context, CancellationToken cancellationToken)
    {
      if (Resource.ParentId == null)
      {
        return null;
      }

      return await Pool.Database.RunTransaction((transaction) =>
      {
        FileNodeResource? folder = Pool.Nodes.GetById(transaction, (long)Resource.ParentId);
        if (folder == null)
        {
          return null;
        }

        return (BlobFolderHandle)Pool.ResolveHandle(folder);
      }, cancellationToken);
    }

    protected override async Task InternalTrash(Context context, CancellationToken cancellationToken)
    {
      await Pool.Database.RunTransaction((transaction) =>
      {
        if (Resource.ParentId != null)
        {
          FileNodeResource? parentNode = Pool.Nodes.GetById(transaction, (long)Resource.ParentId);

          if (parentNode != null)
          {

          }
        }

        string randomChar; //= RandomNumberGenerator.GetHexString(1024);
        do
        {
          randomChar = RandomNumberGenerator.GetHexString(1024);
        }
        while (Pool.Nodes.GetByName(transaction, randomChar, Pool.Nodes.GetByName(transaction, TRASH_NAME, null)) != null);

        FileNodeResource trashNode = Pool.Nodes.CreateFolder(transaction, randomChar, Resource);
        Pool.Nodes.UpdateParentFolder(transaction, Resource, trashNode);
      }, cancellationToken);
    }

    protected override IAsyncEnumerable<Handle> InternalScan(Context context, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task InternalMoveHere(Context context, Handle handle, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }
  }
}
