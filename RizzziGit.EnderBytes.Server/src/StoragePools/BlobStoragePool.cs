namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Buffer;
using Database;

public sealed class BlobStoragePool(StoragePoolManager manager, StoragePoolResource storagePool, FileStream blob) : StoragePool(manager, storagePool, StoragePoolType.Blob)
{
  private sealed record DirectoryHandle(
    BlobStorageFileResource Directory,
    List<BlobStorageFileResource> Files,
    int CurrentIndex
  );

  public readonly Database Database = manager.Server.Resources.MainDatabase;
  public readonly BlobStorageFileResource.ResourceManager Files = manager.Server.Resources.BlobStorageFiles;
  public readonly BlobStorageFileVersionResource.ResourceManager FileVersions = manager.Server.Resources.BlobStorageFileVersions;
  public readonly KeyResource.ResourceManager Keys = manager.Server.Resources.Keys;
  public readonly FileStream Blob = blob;

  private readonly Dictionary<long, DirectoryHandle> Directories = [];

  public override Task ChangeOwner(string[] path, UserResource user, CancellationToken cancellationToken) => Database.RunTransaction(async (transaction, cancellationToken) =>
  {
    BlobStorageFileResource? file = null;

    foreach (string pathEntry in path)
    {
      file = Files.Get(transaction, Resource, file, pathEntry);

      if (file == null)
      {
        break;
      }
    }

    if (file == null)
    {
      throw new ArgumentException("Invalid path.", nameof(path));
    }

    await Files.UpdateOwner(transaction, file, user, cancellationToken);
  }, cancellationToken);

  public override Task Delete(string[] path, CancellationToken cancellationToken) => Database.RunTransaction(async (transaction, cancellationToken) =>
  {
    BlobStorageFileResource? file = null;

    foreach (string pathEntry in path)
    {
      file = Files.Get(transaction, Resource, file, pathEntry);

      if (file == null)
      {
        break;
      }
    }

    if (file == null)
    {
      throw new ArgumentException("Invalid path.", nameof(path));
    }

    await Files.Delete(transaction, file, cancellationToken);
  }, cancellationToken);

  public override Task DirectoryClose(uint handle, CancellationToken cancellationToken)
  {
    lock (this)
    {
      Directories.Remove(handle);
    }
    return Task.CompletedTask;
  }

  public override async Task DirectoryCreate(string[] parentPath, string name, CancellationToken cancellationToken)
  {
    lock (this)
    {
      foreach (KeyValuePair<long, DirectoryHandle> entry in Directories)
      {
        var (id, handle) = entry;

        // handle.Files.Add()
      }
    }
  }

  public override Task<uint> DirectoryOpen(string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<Information> DirectoryRead(uint handle, long length, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task DirectoryRemove(string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<DirectoryInformation> DirectoryStat(string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task FileClose(uint handle, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task FileCreate(string[] parentPath, string name, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<uint> FileOpen(string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<Buffer> FileRead(uint handle, long length, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task FileSeek(uint handle, long position, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<FileInformation> FileStat(uint handle, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task FileWrite(uint handle, Buffer buffer, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<Information[]> ListTrash(CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<Information> Stat(string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task SymbolicLinkCreate(string[] parentPath, string name, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<string> SymbolicLinkRead(string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<SymbolicLinkInformation> SymbolicLinkStat(string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task Trash(string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}
