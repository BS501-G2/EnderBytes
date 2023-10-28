namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Buffer;

public sealed class PhysicalStoragePool : StoragePool
{
  public PhysicalStoragePool(StoragePoolManager manager, StoragePoolResource storagePool) : base(manager, storagePool, StoragePoolType.Physical)
  {
  }

  public override Task ChangeOwner(string[] path, UserResource user, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task Delete(string[] path, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task DirectoryClose(uint handle, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  public override Task DirectoryCreate(string[] parentPath, string name, CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
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
