namespace RizzziGit.EnderBytes.StoragePools;

using Connections;
using Buffer;

public abstract partial class StoragePool
{
  [Flags]
  public enum Access : byte
  {
    Read = 1 << 0,
    Write = 1 << 1,
    Exclusive = 1 << 2,

    ReadWrite = Read | Write,
    ExclusiveReadWrite = Exclusive | ReadWrite
  }

  [Flags]
  public enum Mode : byte
  {
    TruncateToZero = 1 << 0,
    Append = 1 << 1,
    NewSnapshot = 1 << 2
  }

  public abstract record FileInformation(long Id, string Name, long CreateTime, long? ModifyTime, long? AccessTime)
  {
    public sealed record File(long Id, string Name, long CreateTime, long? ModifyTime, long? AccessTime, long Size) : FileInformation(Id, Name, CreateTime, ModifyTime, AccessTime);
    public sealed record Folder(long Id, string Name, long CreateTime, long? ModifyTime, long? AccessTime) : FileInformation(Id, Name, CreateTime, ModifyTime, AccessTime);
    public sealed record SymbolicLink(long Id, string Name, long CreateTime, long? ModifyTime, long? AccessTime, Path TargetPath) : FileInformation(Id, Name, CreateTime, ModifyTime, AccessTime);
  }

  public sealed record FileSnapshotInformation(long Id, long OwnerUserId, long CreateTime, long ModifyTime);
  public sealed record TrashEntryInformation(long Id, long TrashTime);

  protected abstract Task<FileInformation> Internal_Stat(Context context, Path path, CancellationToken cancellationToken);
  protected abstract Task Internal_Delete(Context context, Path path, CancellationToken cancellationToken);

  protected abstract Task<FileInformation[]> Internal_FolderScan(Context context, Path path, CancellationToken cancellationToken);
  protected abstract Task Internal_FolderCreate(Context context, Path path, CancellationToken cancellationToken);

  protected abstract Task<long> Internal_FileOpen(Context context, Path path, long snapshotId, Mode mode, Access access, CancellationToken cancellationToken);
  protected abstract Task<Buffer> Internal_FileRead(Context context, long handleId, long Length, CancellationToken cancellationToken);
  protected abstract Task Internal_FileWrite(Context context, long handleId, Buffer buffer, CancellationToken cancellationToken);
  protected abstract Task Internal_FileClose(Context context, long handleId, bool rollback, CancellationToken cancellationToken);
  protected abstract Task Internal_FileSetSize(Context context, long handleId, long size, CancellationToken cancellationToken);
  protected abstract Task Internal_FileSeek(Context context, long handleId, long position, CancellationToken cancellationToken);
  protected abstract Task<long> Internal_FileTell(Context context, long handleId, CancellationToken cancellationToken);

  protected abstract Task<long> Internal_FileSnapshotCreate(Context context, Path path, long? baseSnapshotId, CancellationToken cancellationToken);
  protected abstract Task<long> Internal_FileSnapshotDelete(Context context, Path path, long snapshotId, CancellationToken cancellationToken);
  protected abstract Task<FileSnapshotInformation[]> Internal_FileSnapshotList(Context context, Path path, long snapshotId, CancellationToken cancellationToken);

  protected abstract Task<Path> Internal_SymbolicLinkRead(Context context, Path path, CancellationToken cancellationToken);
  protected abstract Task Internal_SymbolicLinkCreate(Context context, Path path, Path target, CancellationToken cancellationToken);

  protected abstract Task Internal_Trash(Context context, Path path, CancellationToken cancellationToken);
  protected abstract Task Internal_TrashRestore(Context context, long trashId, Path restorePath, CancellationToken cancellationToken);
  protected abstract Task Internal_TrashDelete(Context context, long trashId, CancellationToken cancellationToken);
  protected abstract Task<TrashEntryInformation[]> Internal_TrashList(Context context, CancellationToken cancellationToken);
  protected abstract Task<FileInformation> Internal_TrashStat(Context context, long trashId, Path path, CancellationToken cancellationToken);
  protected abstract Task<long> Internal_TrashFileOpen(Context context, long trashId, Path path, long snapshotId, CancellationToken cancellationToken);
  protected abstract Task<FileSnapshotInformation[]> Internal_TrashFileSnapshotList(Context context, long trashId, Path path, CancellationToken cancellationToken);
  protected abstract Task<FileInformation[]> Internal_TrashFolderScan(Context context, long trashId, Path path, CancellationToken cancellationToken);
  protected abstract Task<Path> Internal_TrashSymbolicLinkRead(Context context, long trashId, Path path, CancellationToken cancellationToken);

  private Task<FileInformation> Stat(Context context, Path path, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_Stat(context, path, cancellationToken);
  }, cancellationToken);

  private Task Delete(Context context, Path path, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_Delete(context, path, cancellationToken);
  }, cancellationToken);

  private Task<FileInformation[]> FolderScan(Context context, Path path, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_FolderScan(context, path, cancellationToken);
  }, cancellationToken);

  private Task FolderCreate(Context context, Path path, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_FolderCreate(context, path, cancellationToken);
  }, cancellationToken);

  private Task<long> FileOpen(Context context, Path path, long snapshotId, Mode mode, Access access, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_FileOpen(context, path, snapshotId, mode, access, cancellationToken);
  }, cancellationToken);

  private Task<Buffer> FileRead(Context context, long handleId, long length, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_FileRead(context, handleId, length, cancellationToken);
  }, cancellationToken);

  private Task FileWrite(Context context, long handleId, Buffer buffer, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_FileWrite(context, handleId, buffer, cancellationToken);
  }, cancellationToken);

  private Task FileClose(Context context, long handleId, bool rollback, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return FileClose(context, handleId, rollback, cancellationToken);
  }, cancellationToken);

  private Task FileSetSize(Context context, long handleId, long size, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_FileSetSize(context, handleId, size, cancellationToken);
  }, cancellationToken);

  private Task FileSeek(Context context, long handleId, long position, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_FileSeek(context, handleId, position, cancellationToken);
  }, cancellationToken);

  private Task<long> FileTell(Context context, long handleId, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_FileTell(context, handleId, cancellationToken);
  }, cancellationToken);

  private Task<long> FileSnapshotCreate(Context context, Path path, long? baseSnapshotId, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_FileSnapshotCreate(context, path, baseSnapshotId, cancellationToken);
  }, cancellationToken);

  private Task<long> FileSnapshotDelete(Context context, Path path, long snapshotId, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_FileSnapshotDelete(context, path, snapshotId, cancellationToken);
  }, cancellationToken);

  private Task<FileSnapshotInformation[]> FileSnapshotList(Context context, Path path, long snapshotId, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_FileSnapshotList(context, path, snapshotId, cancellationToken);
  }, cancellationToken);

  private Task<Path> SymbolicLinkRead(Context context, Path path, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_SymbolicLinkRead(context, path, cancellationToken);
  }, cancellationToken);

  private Task SymbolicLinkCreate(Context context, Path path, Path target, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_SymbolicLinkCreate(context, path, target, cancellationToken);
  }, cancellationToken);

  private Task Trash(Context context, Path path, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_Trash(context, path, cancellationToken);
  }, cancellationToken);

  private Task TrashRestore(Context context, long trashId, Path restorePath, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_TrashRestore(context, trashId, restorePath, cancellationToken);
  }, cancellationToken);

  private Task TrashDelete(Context context, long trashId, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_TrashDelete(context, trashId, cancellationToken);
  }, cancellationToken);

  private Task<TrashEntryInformation[]> TrashList(Context context, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_TrashList(context, cancellationToken);
  }, cancellationToken);

  private Task<FileInformation> TrashStat(Context context, long trashId, Path path, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_TrashStat(context, trashId, path, cancellationToken);
  }, cancellationToken);

  private Task<long> TrashFileOpen(Context context, long trashId, Path path, long snapshotId, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_TrashFileOpen(context, trashId, path, snapshotId, cancellationToken);
  }, cancellationToken);

  private Task<FileSnapshotInformation[]> TrashFileSnapshotList(Context context, long trashId, Path path, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_TrashFileSnapshotList(context, trashId, path, cancellationToken);
  }, cancellationToken);

  private Task<FileInformation[]> TrashFolderScan(Context context, long trashId, Path path, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_TrashFolderScan(context, trashId, path, cancellationToken);
  }, cancellationToken);

  private Task<Path> TrashSymbolicLinkRead(Context context, long trashId, Path path, CancellationToken cancellationToken) => TaskQueue.RunTask((cancellationToken) =>
  {
    return Internal_TrashSymbolicLinkRead(context, trashId, path, cancellationToken);
  }, cancellationToken);

  public abstract class Context(StoragePool pool, Connection connection)
  {
    public readonly StoragePool Pool = pool;
    public readonly Connection Connection = connection;

    public Task<FileInformation> Stat(Path path, CancellationToken cancellationToken) => Pool.Stat(this, path, cancellationToken);
    public Task Delete(Path path, CancellationToken cancellationToken) => Pool.Delete(this, path, cancellationToken);

    public Task<FileInformation[]> FolderScan(Path path, CancellationToken cancellationToken) => Pool.FolderScan(this, path, cancellationToken);
    public Task FolderCreate(Path path, CancellationToken cancellationToken) => Pool.FolderCreate(this, path, cancellationToken);

    public Task<long> FileOpen(Path path, long snapshotId, Mode mode, Access access, CancellationToken cancellationToken) => Pool.FileOpen(this, path, snapshotId, mode, access, cancellationToken);
    public Task<Buffer> FileRead(long handleId, long length, CancellationToken cancellationToken) => Pool.FileRead(this, handleId, length, cancellationToken);
    public Task FileWrite(long handleId, Buffer buffer, CancellationToken cancellationToken) => Pool.FileWrite(this, handleId, buffer, cancellationToken);
    public Task FileClose(long handleId, bool rollback, CancellationToken cancellationToken) => Pool.FileClose(this, handleId, rollback, cancellationToken);
    public Task FileSetSize(long handleId, long size, CancellationToken cancellationToken) => Pool.FileSetSize(this, handleId, size, cancellationToken);
    public Task FileSeek(long handleId, long position, CancellationToken cancellationToken) => Pool.FileSeek(this, handleId, position, cancellationToken);
    public Task<long> FileTell(long handleId, CancellationToken cancellationToken) => Pool.FileTell(this, handleId, cancellationToken);

    public Task<long> FileSnapshotCreate(Path path, long? baseSnapshotId, CancellationToken cancellationToken) => Pool.FileSnapshotCreate(this, path, baseSnapshotId, cancellationToken);
    public Task<long> FileSnapshotDelete(Path path, long snapshotId, CancellationToken cancellationToken) => Pool.FileSnapshotDelete(this, path, snapshotId, cancellationToken);
    public Task<FileSnapshotInformation[]> FileSnapshotList(Path path, long snapshotId, CancellationToken cancellationToken) => Pool.FileSnapshotList(this, path, snapshotId, cancellationToken);

    public Task<Path> SymbolicLinkRead(Path path, CancellationToken cancellationToken) => Pool.SymbolicLinkRead(this, path, cancellationToken);
    public Task SymbolicLinkCreate(Path path, Path target, CancellationToken cancellationToken) => Pool.SymbolicLinkCreate(this, path, target, cancellationToken);

    public Task Trash(Path path, CancellationToken cancellationToken) => Pool.Trash(this, path, cancellationToken);
    public Task TrashRestore(long trashId, Path restorePath, CancellationToken cancellationToken) => Pool.TrashRestore(this, trashId, restorePath, cancellationToken);
    public Task TrashDelete(long trashId, CancellationToken cancellationToken) => Pool.TrashDelete(this, trashId, cancellationToken);
    public Task<TrashEntryInformation[]> TrashList(CancellationToken cancellationToken) => Pool.TrashList(this, cancellationToken);
    public Task<FileInformation> TrashStat(long trashId, Path path, CancellationToken cancellationToken) => Pool.TrashStat(this, trashId, path, cancellationToken);
    public Task<long> TrashFileOpen(long trashId, Path path, long snapshotId, CancellationToken cancellationToken) => Pool.TrashFileOpen(this, trashId, path, snapshotId, cancellationToken);
    public Task<FileSnapshotInformation[]> TrashFileSnapshotList(long trashId, Path path, CancellationToken cancellationToken) => Pool.TrashFileSnapshotList(this, trashId, path, cancellationToken);
    public Task<FileInformation[]> TrashFolderScan(long trashId, Path path, CancellationToken cancellationToken) => Pool.TrashFolderScan(this, trashId, path, cancellationToken);
    public Task<Path> TrashSymbolicLinkRead(long trashId, Path path, CancellationToken cancellationToken) => Pool.TrashSymbolicLinkRead(this, trashId, path, cancellationToken);
  }
}
