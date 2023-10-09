using System.Data.SQLite;

namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Database;
using Buffer;

public sealed class BlobStoragePool(EnderBytesServer server, uint requireType, StoragePoolResource resource) : StoragePool(server, requireType, resource)
{
  private BlobStorageFileResource.ResourceManager Files => Server.Resources.BlobStorageFiles;
  private BlobStorageFileVersionResource.ResourceManager Versions => Server.Resources.BlobStorageFileVersions;
  private BlobStorageKeyResource.ResourceManager Keys => Server.Resources.BlobStorageKeys;
  private BlobStorageFileBlobResource.ResourceManager Blob => Server.Resources.BlobStorageFileBlobs;
  private Database Database => Server.Resources.RequireDatabase();

  public override Task<byte> ChangeMode(UserResource initiator, string[] path, int mode, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<byte> ChangeOwnership(UserResource initiator, string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<byte> CloseFileHandle(UserResource initiator, uint handle, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<byte> CreateDirectory(UserResource initiator, string[] path, int mode, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override async Task<byte> CreateFile(UserResource initiator, string[] path, int mode, CancellationToken cancellationToken)
  {
    if (path.Length == 0 || path[0] == TRASH_NAME)
    {
      return STATUS_EINVAL;
    }

    return await Database.RunTransaction(async (connection, cancellationToken) =>
    {
      BlobStorageFileResource? parentFolder = null;
      if (path.Length > 1)
      {
        foreach (string pathEntry in path)
        {
          parentFolder = await Files.GetByName(connection, Resource, pathEntry, parentFolder, cancellationToken);

          if (parentFolder == null)
          {
            break;
          }
        }

        if (parentFolder == null)
        {
          return STATUS_ENOENT;
        }
        else if (parentFolder.Type != BlobStorageFileResource.TYPE_FOLDER)
        {
          return STATUS_ENOTDIR;
        }
      }

      await Files.Create(connection, Resource, initiator, parentFolder, BlobStorageFileResource.TYPE_FILE, path.Last(), mode, Server.Config.DefaultBlobStorageFileBufferSize, cancellationToken);
      return STATUS_OK;
    }, cancellationToken);
  }

  public override async Task<byte> CreateSymbolicLink(UserResource initiator, string[] path, string target, CancellationToken cancellationToken)
  {
    if (path.Length == 0 || path[0] == TRASH_NAME)
    {
      return STATUS_EINVAL;
    }

    return await Database.RunTransaction(async (connection, cancellationToken) =>
    {
      BlobStorageFileResource? parentFolder = null;
      if (path.Length > 1)
      {
        foreach (string pathEntry in path)
        {
          parentFolder = await Files.GetByName(connection, Resource, pathEntry, parentFolder, cancellationToken);

          if (parentFolder == null)
          {
            break;
          }
        }

        if (parentFolder == null)
        {
          return STATUS_ENOENT;
        }
        else if (parentFolder.Type != BlobStorageFileResource.TYPE_FOLDER)
        {
          return STATUS_ENOTDIR;
        }
      }

      if (await Files.GetByName(connection, Resource, path.Last(), parentFolder, cancellationToken) != null)
      {
        return STATUS_EEXIST;
      }

      await Files.Create(connection, Resource, initiator, parentFolder, BlobStorageFileResource.TYPE_SYMBOLIC_LINK, path.Last(), 0, target.Length, cancellationToken);
      return STATUS_OK;
    }, cancellationToken);
  }

  public override async Task<byte> DeleteFile(UserResource initiator, string[] path, CancellationToken cancellationToken)
  {
    if (path.Length == 0)
    {
      return STATUS_EACCESS;
    }

    return await Database.RunTransaction(async (connection, cancellationToken) =>
    {
      bool trashMode = false;
      BlobStorageFileResource? file = null;
      for (int index = 0; index < path.Length; index++)
      {
        if (index == 0 && path[index] == TRASH_NAME)
        {
          trashMode = true;
          continue;
        }

        file = await Files.GetByName(connection, Resource, path[index], file, cancellationToken);

        if (file == null)
        {
          return STATUS_ENOENT;
        }
        else if (index != (path.Length - 1) && file.Type != BlobStorageFileResource.TYPE_FOLDER)
        {
          return STATUS_ENOTDIR;
        }
      }

      if (file == null)
      {
        return STATUS_EACCESS;
      }

      if (trashMode)
      {
        await Files.Delete(connection, file, cancellationToken);
      }
      else
      {
        await Files.Trash(connection, file, cancellationToken);
      }

      return STATUS_OK;
    }, cancellationToken);
  }

  public override Task<byte> InsertToFileHandle(UserResource initiator, uint handle, Buffer buffer, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<(byte status, uint handle)> OpenFile(UserResource initiator, string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override async Task<(byte status, IAsyncEnumerable<FileInfo> entries)> ReadDirectory(UserResource initiator, string[] path, CancellationToken cancellationToken)
  {
    return await Database.RunTransaction<(byte status, IAsyncEnumerable<FileInfo> entries)>(async (connection, cancellationToken) =>
    {
    }, cancellationToken);
  }

  public override Task<(byte status, Buffer buffer)> ReadFromFileHandle(UserResource initiator, uint handle, long count, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<(byte status, string target)> ReadSymbolicLink(UserResource initiator, string[] path, string target, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<byte> RemoveDirectory(UserResource initiator, string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<byte> SeekFileHandle(UserResource initiator, uint handle, long offset, SeekOrigin seekOrigin, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task Shutdown(CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task Startup(CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<(byte status, FileInfo? stats)> Stats(UserResource initiator, string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<byte> WriteToFileHandle(UserResource initiator, uint handle, Buffer buffer, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}
