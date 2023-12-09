namespace RizzziGit.EnderBytes.StoragePools;

using Resources;
using Buffer;
using Connections;

public abstract partial class StoragePool
{
  public partial interface INode
  {
    public interface IFile : INode
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

      public interface ISnapshot
      {
        public IFile File { get; }

        public StoragePool Pool => File.Pool;
      }

      public interface IStream
      {
        public IFile File { get; }
        public ISnapshot Snapshot { get; }

        public StoragePool Pool => File.Pool;

        protected Task ISeek(KeyResource.Transformer transformer, long position);
        protected Task ISetSize(KeyResource.Transformer transformer, long size);
        protected Task<Buffer> IRead(KeyResource.Transformer transformer, long count);
        protected Task IWrite(KeyResource.Transformer transformer, Buffer buffer);
        protected Task IClose(KeyResource.Transformer poolTransformer);

        public Task Seek(KeyResource.Transformer transformer, long position) => ISeek(transformer, position);
        public Task SetSize(KeyResource.Transformer transformer, long size) => ISetSize(transformer, size);
        public Task<Buffer> Read(KeyResource.Transformer transformer, long count) => IRead(transformer, count);
        public Task Write(KeyResource.Transformer transformer, Buffer buffer) => IWrite(transformer, buffer);
        public Task Close(KeyResource.Transformer poolTransformer) => IClose(poolTransformer);
      }

      Task<IStream> IOpen(KeyResource.Transformer transformer, Access access, Mode mode);
    }
  }
}
