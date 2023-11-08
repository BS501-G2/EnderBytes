using System.Security.Cryptography;

namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Buffer;
using Database;
using Connections;
using Utilities;

public sealed class BlobStoragePool(StoragePoolManager manager, StoragePoolResource storagePool, FileStream blob) : StoragePool(manager, storagePool, StoragePoolType.Blob)
{
  private class FileHandle(BlobFileResource file, FileAccess access)
  {
    public readonly BlobFileResource File = file;
    public readonly FileAccess Access = access;
    public long Position = 0;
  }

  private readonly FileStream Blob = blob;
  private readonly Database Database = manager.Server.Resources.MainDatabase;
  private readonly BlobFileResource.ResourceManager Files = manager.Server.Resources.Files;
  private readonly BlobFileSnapshotResource.ResourceManager Snapshots = manager.Server.Resources.FileSnapshots;
  private readonly BlobFileKeyResource.ResourceManager Keys = manager.Server.Resources.FileKeys;
  private readonly BlobFileDataResource.ResourceManager Data = manager.Server.Resources.FileData;

  public override async Task FileCreate(UserAuthenticationResource userAuthentication, byte[] hashCache, string[] path, CancellationToken cancellationToken)
  {
    await Database.RunTransaction((transaction) =>
    {
      BlobFileResource? parentFolder = null;
      foreach (string pathEntry in path.SkipLast(1))
      {
        parentFolder = Files.GetByName(transaction, Resource, parentFolder, pathEntry);
        if (parentFolder == null)
        {
          throw new PathNotFoundException();
        }
        else if (parentFolder.Type == BlobFileType.File)
        {
          throw new NotADirectoryException();
        }
      }

      if (Files.GetByName(transaction, Resource, parentFolder, path.Last()) != null)
      {
        throw new PathFileExistsException();
      }

      Files.CreateFile(transaction, Resource, parentFolder, userAuthentication, path.Last(), hashCache);
    }, cancellationToken);
  }

  private readonly Dictionary<long, FileHandle> Handles = [];

  public override Task<long> FileOpen(string[] path, FileAccess fileAccess, CancellationToken cancellationToken) => Database.RunTransaction((transaction) =>
  {
    BlobFileResource? file = null;
    foreach (string pathEntry in path)
    {
      file = Files.GetByName(transaction, Resource, file, pathEntry);

      if (file == null)
      {
        throw new PathNotFoundException();
      }
    }

    if ((from handle in Handles where handle.Value.File == file && ((handle.Value.Access & FileAccess.Write) == FileAccess.Write) select handle.Value).Any())
    {
      throw new ResourceInUseException();
    }

    long id;

    do { id = Random.Shared.NextInt64(); } while (!Handles.TryAdd(id, new(file!, FileAccess.ReadWrite)));
    return id;
  }, cancellationToken);

  public override Task<Buffer> FileRead(long handleId, long size, UserAuthenticationResource userAuthentication, byte[] hashCache, CancellationToken cancellationToken) => Database.RunTransaction((transaction) =>
  {
    Buffer output = Buffer.Empty();

    if (Handles.TryGetValue(handleId, out var handle))
    {
      if ((handle.Access & FileAccess.Read) == 0)
      {
        throw new InvalidOperationException();
      }
      else if (!handle.File.IsValid)
      {
        throw new DeletedException();
      }

      BlobFileKeyResource key = Keys.Get(transaction, handle.File, userAuthentication) ?? throw new MissingKeyException();
      while (size > 0)
      {
        int offset = (int)(handle.Position / BlobFileKeyResource.BUFFER_SIZE);
        BlobFileDataResource? data = Data.GetByFile(transaction, handle.File, offset);
        if (data == null)
        {
          break;
        }

        Blob.Seek(data.BlobAddress, SeekOrigin.Begin);
        byte[] bytes = new byte[BlobFileKeyResource.BUFFER_SIZE];
        Blob.ReadExactly(bytes);
        byte[] decrypted = key.Decrypt(bytes, hashCache);

        int blockOffset = (int)(handle.Position % BlobFileKeyResource.BUFFER_SIZE);
        Buffer selected = Buffer.From(decrypted, blockOffset, (int)long.Min(decrypted.Length - blockOffset, size));
        output.Append(selected);
        size -= selected.Length;
      }

      handle.Position = long.Min(handle.Position + output.Length, handle.File.Size);
    }

    return output;
  }, cancellationToken);

  public override Task FileWrite(long handleId, Buffer buffer, UserAuthenticationResource userAuthentication, byte[] hashCache, CancellationToken cancellationToken) => Database.RunTransaction((transaction) =>
  {
    if (Handles.TryGetValue(handleId, out var handle))
    {
      if ((handle.Access & FileAccess.Write) == 0)
      {
        throw new InvalidOperationException();
      }
      else if (!handle.File.IsValid)
      {
        throw new DeletedException();
      }

      BlobFileKeyResource key = Keys.Get(transaction, handle.File, userAuthentication) ?? throw new MissingKeyException();
      int offset = (int)handle.Position / BlobFileKeyResource.BUFFER_SIZE;

      BlobFileDataResource? data = Data.GetByFile(transaction, handle.File, offset);
    }
  }, cancellationToken);

  public override Task FileTruncate(long handleId, Buffer buffer, UserAuthenticationResource userAuthentication, byte[] hashCache, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  // private (BlobFileResource? parent, BlobFileResource? file) Find(DatabaseTransaction transaction, string[] path)
  // {
  //   BlobFileResource? parent = null;
  //   BlobFileResource? file = null;
  //   foreach (string pathEntry in path)
  //   {
  //     parent = file;
  //     file = Files.GetByName(transaction, Resource, file, pathEntry);

  //     if (file == null)
  //     {
  //       parent = null;
  //       break;
  //     }
  //   }

  //   return (parent, file);
  // }
}
