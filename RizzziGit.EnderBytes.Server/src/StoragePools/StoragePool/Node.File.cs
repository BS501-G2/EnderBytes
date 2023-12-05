namespace RizzziGit.EnderBytes.StoragePools;

using Resources;

public abstract partial class StoragePool
{
  public abstract partial class Node
  {
    public abstract partial class File : Node
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

      protected abstract Task<Stream> Internal_Open(KeyResource.Transformer transformer, Access access, Mode mode);
    }
  }
}
