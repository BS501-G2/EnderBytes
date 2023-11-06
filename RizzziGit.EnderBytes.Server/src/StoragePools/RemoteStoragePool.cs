namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Buffer;
using Connections;

public sealed class RemoteStoragePool : StoragePool
{
  public RemoteStoragePool(StoragePoolManager manager, StoragePoolResource storagePool) : base(manager, storagePool, StoragePoolType.Remote)
  {
  }

  public override Task ChangeOwner(Connection connection, string[] path, UserResource user, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task Delete(Connection connection, string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task DirectoryClose(Connection connection, uint handle, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task DirectoryCreate(Connection connection, string[] parentPath, string name, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<uint> DirectoryOpen(Connection connection, string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<Information> DirectoryRead(Connection connection, uint handle, long length, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task DirectoryRemove(Connection connection, string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<DirectoryInformation> DirectoryStat(Connection connection, string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task FileClose(Connection connection, uint handle, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task FileCreate(Connection connection, string[] parentPath, string name, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<uint> FileOpen(Connection connection, string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<Buffer> FileRead(Connection connection, uint handle, long length, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task FileSeek(Connection connection, uint handle, long position, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<FileInformation> FileStat(Connection connection, uint handle, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task FileWrite(Connection connection, uint handle, Buffer buffer, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<Information[]> ListTrash(Connection connection, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task Restore(Connection connection, Information information, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<Information> Stat(Connection connection, string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task SymbolicLinkCreate(Connection connection, string[] parentPath, string name, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<string> SymbolicLinkRead(Connection connection, string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task<SymbolicLinkInformation> SymbolicLinkStat(Connection connection, string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task Trash(Connection connection, string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}
