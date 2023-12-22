using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Services;

using Collections;
using Buffer;
using Utilities;
using Records;

[Flags]
public enum HubFileAccess : byte
{
  Read = 1 << 0,
  Write = 1 << 1,
  Exclusive = 1 << 2,

  ReadWrite = Read | Write,
  ExclusiveReadWrite = Exclusive | ReadWrite
}

[Flags]
public enum HubFileMode : byte
{
  TruncateToZero = 1 << 0,
  Append = 1 << 1
}

public sealed partial class StorageHubService
{
  public abstract partial class Hub(StorageHubService service, long hubId, KeyGeneratorService.Transformer.Key hubKey) : Lifetime($"{hubId}", service.Logger)
  {
    public abstract class TrashItem(Hub hub, long id)
    {
      public readonly Hub Hub = hub;
      public readonly long Id = id;
    }

    public readonly StorageHubService Service = service;
    public readonly long HubId = hubId;
    public readonly KeyGeneratorService.Transformer.Key HubKey = hubKey;

    private readonly WeakDictionary<long, TrashItem> TrashItems = [];
    private readonly WeakDictionary<long, FileHandle> FileHandles = [];
    private readonly WeakDictionary<FileHandle, List<FileHandle.LazyBuffer>> FileHandleCache = [];

    protected abstract Task<Node.Folder> Internal_GetRootFolder(ConnectionService.Connection connection);
    protected abstract Task<TrashItem> Internal_ScanTrash(ConnectionService.Connection connection);

    public Task<Node.Folder> GetRoot(ConnectionService.Connection connection) => RunTask((_) => Internal_GetRootFolder(connection));
  }
}
