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

        Task ISeek(Connection transformer, long position);
        Task ISetSize(Connection transformer, long size);
        Task<Buffer> IRead(Connection transformer, long count);
        Task IWrite(Connection transformer, Buffer buffer);
      }

      Task<IStream> IOpen(Connection transformer, Access access, Mode mode);
    }
  }
}
