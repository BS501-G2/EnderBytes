namespace RizzziGit.EnderBytes.StoragePools;

using Buffer;
using Resources;

public abstract partial class StoragePool
{
  public abstract partial class Node
  {
    public abstract partial class File
    {
      public abstract class Snapshot
      {
        public abstract Snapshot File { get; }

        public StoragePool Handle => File.Handle;
      }

      public abstract class Stream
      {
        public abstract File File { get; }
        public abstract Snapshot Snapshot { get; }

        public StoragePool Handle => File.Handle;

        protected abstract Task Internal_Seek(KeyResource.Transformer transformer, long position);
        protected abstract Task Internal_SetSize(KeyResource.Transformer transformer, long size);
        protected abstract Task<Buffer> Internal_Read(KeyResource.Transformer transformer, long count);
        protected abstract Task Internal_Write(KeyResource.Transformer transformer, Buffer buffer);
      }
    }
  }
}
