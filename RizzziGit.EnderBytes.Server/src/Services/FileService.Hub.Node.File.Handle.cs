namespace RizzziGit.EnderBytes.Services;

using Framework.Memory;
using Framework.Collections;

using Resources;

public sealed partial class FileService
{
  public sealed partial class Hub
  {
    public abstract partial class Node
    {
      public sealed partial class File
      {
        public sealed class Handle(File file, Snapshot snapshot)
        {
          public readonly File File = file;
          public readonly Snapshot Snapshot = snapshot;

          public Hub Hub => File.Hub;
          public FileService FileService => Hub.Service;

          public async Task<CompositeBuffer> Read(UserAuthenticationResource.Token token)
          {
            return [];
          }
        }

        // private sealed class BufferCache
        // {
        //   public BufferCache(long begin, long end, CompositeBuffer? buffer, bool toWrite)
        //   {
        //     Begin = begin;
        //     End = end;
        //     Buffer = buffer;
        //     ToWrite = toWrite;
        //   }

        //   public readonly long Begin;
        //   public readonly long End;

        //   private CompositeBuffer? UnderlyingBuffer;

        //   public long Length => End - Begin;
        //   public bool ToWrite;

        //   public CompositeBuffer? Buffer
        //   {
        //     get => UnderlyingBuffer;
        //     set
        //     {
        //       if ((value != null) && (value.Length != Length))
        //       {
        //         throw new ArgumentException("CompositeBuffer length does not match the BufferCache length.", nameof(value));
        //       }

        //       UnderlyingBuffer = value;
        //     }
        //   }
        // }
      }
    }
  }
}
