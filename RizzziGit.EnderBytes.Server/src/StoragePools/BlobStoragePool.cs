using System.Text;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderBytes.StoragePools;

using Buffer;
using Resources;
using Resources.BlobStorage;

public sealed class BlobStoragePool : StoragePool
{
  private new class FileHandle(StoragePool pool, string[] path, StoragePool.FileHandle.FileAccess access) : StoragePool.FileHandle(pool, path, access)
  {
    protected override Task<Buffer> InternalRead(long position, long size, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task InternalTruncate(long size, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task InternalWrite(long position, Buffer buffer, CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task OnRun(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task OnStart(CancellationToken cancellationToken)
    {
      throw new NotImplementedException();
    }

    protected override Task OnStop(System.Exception? exception)
    {
      throw new NotImplementedException();
    }
  }

  public BlobStoragePool(StoragePoolManager manager, StoragePoolResource resource) : base(manager, resource)
  {
    Resources = new(this);
  }

  private readonly BlobStorageResourceManager Resources;

  protected override async Task InternalCreateDirectory(Context context, string[] path, CancellationToken cancellationToken)
  {
    if (path.Length == 0)
    {
      throw new Exception.FileOrFolderExists();
    }

    await Resources.MainDatabase.RunTransaction((transaction) =>
    {
      BlobFileResource? parentFolder = null;

      foreach (string pathEntry in path.SkipLast(1))
      {
        parentFolder = Resources.Files.GetByName(transaction, parentFolder, pathEntry);

        if (parentFolder == null)
        {
          throw new Exception.NoSuchFileOrFolder();
        }
      }

      if (Resources.Files.GetByName(transaction, parentFolder, path.Last()) != null)
      {
        throw new Exception.FileOrFolderExists();
      }

      Resources.Files.CreateFolder(transaction, parentFolder, path.Last());
    }, cancellationToken);
  }

  protected override async Task InternalCreateSymbolicLink(Context context, string[] path, string[] target, CancellationToken cancellationToken)
  {
    if (path.Length == 0)
    {
      throw new Exception.FileOrFolderExists();
    }

    await Resources.MainDatabase.RunTransaction((transaction) =>
    {
      BlobFileResource? parentFolder = null;

      foreach (string pathEntry in path.SkipLast(1))
      {
        parentFolder = Resources.Files.GetByName(transaction, parentFolder, pathEntry);

        if (parentFolder == null)
        {
          break;
        }
      }

      if (parentFolder == null)
      {
        throw new Exception.NoSuchFileOrFolder();
      } else if (Resources.Files.GetByName(transaction, parentFolder, Name) != null)
      {
        throw new Exception.FileOrFolderExists();
      }

      UserKeyResource.Transformer transformer = context.GetTransformer();
      BlobFileResource file = Resources.Files.CreateSymbolicLink(transaction, parentFolder, Name);

      Resources.Maps.Create(
        transaction,
        Resources.Versions.Create(transaction, file, context.User),
        Resources.Data.Create(transaction, Encoding.UTF8.GetBytes(JArray.FromObject(target).ToString()))
      );
    }, cancellationToken);
  }

  protected override async Task<Information> InternalStat(Context context, string[] path, CancellationToken cancellationToken)
  {
    if (path.Length != 0)
    {
      return new Information.Root(null, null);
    }

    return await Resources.MainDatabase.RunTransaction<Information>((transaction) =>
    {
      BlobFileResource? entry = null;

      foreach (string pathEntry in path)
      {
        entry = Resources.Files.GetByName(transaction, entry, pathEntry);

        if (entry == null)
        {
          break;
        }
      }

      if (entry == null)
      {
        throw new Exception.NoSuchFileOrFolder();
      }

      switch (entry.Type)
      {
        case BlobFileType.SymbolicLink: return new Information.SymbolicLink(entry.Name, entry.AccessTime, entry.TrashTime);
        case BlobFileType.Folder: return new Information.Folder(entry.Name, entry.AccessTime, entry.TrashTime);
        case BlobFileType.File:
          {
            foreach (BlobFileVersionResource version in Resources.Versions.StreamByFile(transaction, entry))
            {
              return new Information.File(entry.Name, version.Size, entry.AccessTime, entry.TrashTime);
            }

            return new Information.File(entry.Name, 0, entry.AccessTime, entry.TrashTime);
          }

        default: throw new InvalidOperationException("Invalid resource type.");
      }
    }, cancellationToken);
  }

  protected override async Task InternalDelete(Context context, string[] path, CancellationToken cancellationToken)
  {
    if (path.Length == 0)
    {
      throw new Exception.IsAFolder();
    }

    await Resources.MainDatabase.RunTransaction((transaction) =>
    {
      BlobFileResource? entry = null;

      foreach (string pathEntry in path)
      {
        entry = Resources.Files.GetByName(transaction, entry, pathEntry);

        if (entry == null)
        {
          break;
        }
      }

      if (entry == null)
      {
        throw new Exception.NoSuchFileOrFolder();
      } else if (entry.Type == BlobFileType.Folder)
      {
        throw new Exception.IsAFolder();
      }

      Resources.Files.Delete(transaction, entry);
    }, cancellationToken);
  }

  protected override Task InternalMove(Context context, string[] fromPath, string[] targetParentFolder, CancellationToken cancellationToken)
  {
    if (fromPath.Length == 0)
    {
      throw new Exception.AccessDenied();
    }
  }

  protected override Task InternalCopy(Context context, string[] sourcePath, string[] destinationPath, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task<StoragePool.FileHandle> InternalOpen(Context context, string[] path, StoragePool.FileHandle.FileAccess access, StoragePool.FileHandle.FileMode mode, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task InternalRemoveDirectory(Context context, string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task<Information[]> InternalScanDirectory(Context context, string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task<string[]> InternalReadSymbolicLink(Context context, string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task<long> InternalTrash(Context context, string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  protected override Task InternalRestore(Context context, long trashedFileId, string[]? newPath, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}
