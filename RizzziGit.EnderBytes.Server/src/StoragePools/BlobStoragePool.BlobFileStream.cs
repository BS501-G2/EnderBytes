namespace RizzziGit.EnderBytes.StoragePools;

using Buffer;

public sealed partial class BlobStoragePool
{
  private sealed class BlobFileStream(Handle.File file, Handle.File.Snapshot snapshot, Handle.File.Access access) : Handle.File.Stream(file, snapshot, access)
  {
    protected override Task InternalClose()
    {
      throw new NotImplementedException();
    }

    protected override Task<Buffer> InternalRead(Context context, long position, long size)
    {
      throw new NotImplementedException();
    }

    protected override Task InternalTruncate(Context context, long size)
    {
      throw new NotImplementedException();
    }

    protected override Task InternalWrite(Context context, long position, Buffer buffer)
    {
      throw new NotImplementedException();
    }
  }
}
