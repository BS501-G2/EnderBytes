using System.Data.Common;

namespace RizzziGit.EnderBytes.Resources;

using Framework.Memory;
using Framework.Collections;

using Services;
using Utilities;

public sealed class FileResource(FileResource.ResourceManager manager, FileResource.ResourceData data) : Resource<FileResource.ResourceManager, FileResource.ResourceData, FileResource>(manager, data)
{
  public enum FileType : byte { File, Folder, SymbolicLink }

  [Flags]
  public enum FileHandleFlags : byte
  {
    Read = 1 << 0,
    Write = 1 << 1,
    Exclusive = 1 << 2,

    ReadWrite = Read | Write
  }

  public abstract record FileHandle(ResourceManager Manager, FileResource File, FileSnapshotResource Snapshot, UserAuthenticationResource.UserAuthenticationToken UserAuthenticationToken, FileHandleFlags Flags)
  {
    public long Position => InternalGetPosition();

    protected abstract long InternalGetPosition();

    public abstract bool InternalSeek(ResourceService.Transaction transaction, long newPosition);
  }

  private const string NAME = "File";
  private const int VERSION = 1;

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, FileResource>.ResourceManager
  {
    private sealed record FileHandle : FileResource.FileHandle
    {
      public FileHandle(ResourceManager Manager, FileResource File, FileSnapshotResource Snapshot, UserAuthenticationResource.UserAuthenticationToken? UserAuthenticationToken, FileHandleFlags Flags) : base(Manager, File, Snapshot, UserAuthenticationToken, Flags)
      {
        Manager.Handles.Add(this);
      }

      ~FileHandle() => Manager.Handles.Remove(this);

      private new long Position = 0;

      protected override long InternalGetPosition() => Position;

      public override bool InternalSeek(ResourceService.Transaction transaction, long newPosition)
      {
        throw new NotImplementedException();
      }
    }

    private const string COLUMN_STORAGE_ID = "StorageId";
    private const string COLUMN_KEY = "AesKey";
    private const string COLUMN_PARENT_FILE_ID = "ParentFileId";
    private const string COLUMN_TYPE = "Type";
    private const string COLUMN_NAME = "Name";

    private const string UNIQUE_INDEX_NAME = $"Index_{NAME}_{COLUMN_NAME}";

    public ResourceManager(ResourceService service) : base(service, NAME, VERSION)
    {
      Service.Storages.ResourceDeleted += (transaction, resource, cancellationToken) => base.Delete(transaction, new WhereClause.CompareColumn(COLUMN_STORAGE_ID, "=", resource.Id), cancellationToken);
      ResourceDeleted += (transaction, resource, cancellationToken) => base.Delete(transaction, new WhereClause.CompareColumn(COLUMN_PARENT_FILE_ID, "=", resource.Id), cancellationToken);

      Handles = [];
    }

    private readonly WeakList<FileHandle> Handles;

    protected override FileResource NewResource(ResourceData data) => new(this, data);

    protected override ResourceData CastToData(DbDataReader reader, long id, long createTime, long updateTime) => new(
      id, createTime, updateTime,

      reader.GetInt64(reader.GetOrdinal(COLUMN_STORAGE_ID)),
      reader.GetBytes(reader.GetOrdinal(COLUMN_KEY)),
      reader.GetInt64Optional(reader.GetOrdinal(COLUMN_PARENT_FILE_ID)),
      (FileType)reader.GetByte(reader.GetOrdinal(COLUMN_TYPE)),
      reader.GetString(reader.GetOrdinal(COLUMN_NAME))
    );

    protected override void Upgrade(ResourceService.Transaction transaction, int oldVersion = 0, CancellationToken cancellationToken = default)
    {
      if (oldVersion < 1)
      {
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_STORAGE_ID} bigint not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_KEY} blob not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_PARENT_FILE_ID} bigint null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_TYPE} tinyint not null;");
        SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_NAME} varchar(128) not null;");

        SqlNonQuery(transaction, $"create unique index {UNIQUE_INDEX_NAME} on {NAME}({COLUMN_STORAGE_ID},{COLUMN_PARENT_FILE_ID},{COLUMN_NAME});");
      }
    }

    public bool Move(ResourceService.Transaction transaction, StorageResource storage, FileResource file, FileResource? newParent, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();

      lock (this)
      {
        lock (storage)
        {
          storage.ThrowIfInvalid();
          file.ThrowIfDoesNotBelongTo(storage);

          if (newParent == null)
          {
            return moveParent();
          }

          lock (newParent)
          {
            newParent.ThrowIfDoesNotBelongTo(storage);
            newParent.ThrowIfInvalid();

            FileResource currentParent = newParent;
            while (currentParent != null)
            {
              if (currentParent.Id == file.Id)
              {
                throw new ArgumentException("Moving the file inside the new parent folder closes the loop.", nameof(newParent));
              }
              else if (currentParent.ParentId == null)
              {
                break;
              }

              currentParent = GetById(transaction, (long)currentParent.ParentId, cancellationToken);
            }

            return moveParent();
          }
        }
      }

      bool moveParent()
      {
        cancellationToken.ThrowIfCancellationRequested();

        if ((newParent != null) && (newParent.Type != FileType.Folder))
        {
          throw new ArgumentException("Invalid new parent.", nameof(newParent));
        }

        bool result = Update(transaction, file, new(
          (COLUMN_PARENT_FILE_ID, newParent?.Id),
          (COLUMN_KEY, Service.Storages.EncryptFileKey(transaction, storage, Service.Storages.DecryptFileKey(transaction, storage, file, userAuthenticationToken, FileAccessResource.FileAccessType.ReadWrite, cancellationToken).Key, newParent, userAuthenticationToken, FileAccessResource.FileAccessType.ReadWrite, cancellationToken))
        ), cancellationToken);

        return result;
      }
    }

    public FileResource Create(ResourceService.Transaction transaction, StorageResource storage, FileResource? parent, FileType type, string name, UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();
      lock (this)
      {
        lock (storage)
        {
          storage.ThrowIfInvalid();

          if (parent == null)
          {
            return create();
          }

          lock (parent)
          {
            parent.ThrowIfInvalid();
            parent.ThrowIfDoesNotBelongTo(storage);

            return create();
          }
        }
      }

      FileResource create()
      {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Enum.IsDefined(type))
        {
          throw new ArgumentException("Invalid file type.", nameof(type));
        }
        else if ((parent != null) && (parent.Type != FileType.Folder))
        {
          throw new ArgumentException("Parent is not a folder.", nameof(parent));
        }

        KeyService.AesPair fileKey = Service.Server.KeyService.GetNewAesPair();

        FileResource file = Insert(transaction, new(
          (COLUMN_STORAGE_ID, storage.Id),
          (COLUMN_KEY, Service.Storages.EncryptFileKey(transaction, storage, fileKey, parent, userAuthenticationToken, FileAccessResource.FileAccessType.ReadWrite, cancellationToken)),
          (COLUMN_PARENT_FILE_ID, parent?.Id),
          (COLUMN_NAME, name),
          (COLUMN_TYPE, (byte)type)
        ), cancellationToken);

        Console.WriteLine($"Encrypted file key #{file.Id}: {CompositeBuffer.From(fileKey.Serialize()).ToHexString()}");

        return file;
      }
    }

    public bool Delete(ResourceService.Transaction transaction, StorageResource storage, FileResource file, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      lock (storage)
      {
        storage.ThrowIfInvalid();

        lock (file)
        {
          file.ThrowIfInvalid();
          file.ThrowIfDoesNotBelongTo(storage);

          if (userAuthenticationToken == null)
          {
            return delete();
          }

          lock (userAuthenticationToken)
          {
            userAuthenticationToken.ThrowIfInvalid();

            return delete();
          }

          bool delete()
          {
            Service.Storages.DecryptFileKey(transaction, storage, file, userAuthenticationToken, FileAccessResource.FileAccessType.ReadWrite, cancellationToken);

            return base.Delete(transaction, file, cancellationToken);
          }
        }
      }
    }

    public override bool Delete(ResourceService.Transaction transaction, FileResource file, CancellationToken cancellationToken = default)
    {
      throw new NotSupportedException("Please specify user token.");
    }

    public IEnumerable<FileResource> ScanFolder(ResourceService.Transaction transaction, StorageResource storage, FileResource? folder, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken, CancellationToken cancellationToken = default)
    {
      lock (storage)
      {
        if (folder == null)
        {
          return scanFolder();
        }

        lock (folder)
        {
          return scanFolder();
        }

        IEnumerable<FileResource> scanFolder()
        {
          if (userAuthenticationToken == null)
          {
            return scanFolder();
          }

          lock (userAuthenticationToken)
          {
            return scanFolder();
          }

          IEnumerable<FileResource> scanFolder()
          {
            if (folder != null)
            {
              Service.Storages.DecryptFileKey(transaction, storage, folder, userAuthenticationToken, FileAccessResource.FileAccessType.Read, cancellationToken);
            }

            foreach (FileResource file in Select(transaction, new WhereClause.Nested("and",
              new WhereClause.CompareColumn(COLUMN_STORAGE_ID, "=", storage.Id),
              new WhereClause.CompareColumn(COLUMN_PARENT_FILE_ID, "=", folder?.Id)
            ), null, null, cancellationToken))
            {
              yield return file;
            }

            yield break;
          }
        }
      }
    }

    public FileResource.FileHandle OpenFile(ResourceService.Transaction transaction, StorageResource storage, FileResource file, FileSnapshotResource fileSnapshot, UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken, FileHandleFlags handleFlags, CancellationToken cancellationToken = default)
    {
      lock (storage)
      {
        lock (file)
        {
          if (userAuthenticationToken == null)
          {
            return openFile();
          }

          lock (userAuthenticationToken)
          {
            return openFile();
          }

          FileResource.FileHandle openFile()
          {
            StorageResource.DecryptedFileKeyInfo decryptedFileKeyInfo = Service.Storages.DecryptFileKey(transaction, storage, file, userAuthenticationToken, handleFlags.HasFlag(FileHandleFlags.Write)
              ? FileAccessResource.FileAccessType.ReadWrite
              : FileAccessResource.FileAccessType.Read, cancellationToken);

            if (file.Type != FileType.File)
            {
              throw new ArgumentException("Not a file.", nameof(file));
            }

            foreach (FileHandle fileHandle in Handles)
            {
              if (fileHandle.Flags.HasFlag(FileHandleFlags.Exclusive))
              {
                throw new InvalidOperationException("File is currently locked for exclusive access.");
              }
            }

            return new FileHandle(this, file, handleFlags.HasFlag(FileHandleFlags.Write) ? Service.FileSnapshots.Create(transaction, storage, file, fileSnapshot, userAuthenticationToken, cancellationToken) : fileSnapshot, userAuthenticationToken, handleFlags);
          }
        }
      }
    }
  }

  public new sealed record ResourceData(long Id, long CreateTime, long UpdateTime, long StorageId, byte[] Key, long? ParentId, FileType Type, string Name) : Resource<ResourceManager, ResourceData, FileResource>.ResourceData(Id, CreateTime, UpdateTime);

  ~FileResource()
  {
  }

  public long StorageId => Data.StorageId;
  public byte[] Key => Data.Key;
  public long? ParentId => Data.ParentId;
  public FileType Type => Data.Type;
  public string Name => Data.Name;

  public bool BelongsTo(StorageResource storage)
  {
    lock (this)
    {
      lock (storage)
      {
        return storage.Id == StorageId;
      }
    }
  }

  public void ThrowIfDoesNotBelongTo(StorageResource storage)
  {
    if (!BelongsTo(storage))
    {
      throw new ArgumentException("The specified file does not belong to storage.", nameof(storage));
    }
  }
}
