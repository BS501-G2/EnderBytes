namespace RizzziGit.EnderBytes.Utilities;

using Buffer;
using Resources;

internal class BidirectionalStream : Stream
{
  public static (BidirectionalStream stream1, BidirectionalStream stream2) Create()
  {
    var (channel1, channel2) = BidirectionalChannel<byte[]>.CreateUnbounded();

    return (
      new(channel1),
      new(channel2)
    );
  }

  private BidirectionalStream(BidirectionalChannel<byte[]> channel)
  {
    Channel = channel;
    ReadQueue = Buffer.Empty();
  }

  private readonly BidirectionalChannel<byte[]> Channel;

  public override bool CanRead => true;
  public override bool CanSeek => false;
  public override bool CanWrite => true;
  public override long Length => 0;

  public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

  private readonly Buffer ReadQueue;
  public override int Read(byte[] buffer, int offset, int count)
  {
    if (ReadQueue.Length != 0)
    {
      return (int)ReadQueue.TruncateStart(buffer, offset, (int)long.Min(ReadQueue.Length, count));
    }

    byte[] item;
    {
      Task<byte[]> task = Channel.Reader.ReadAsync(CancellationToken.None).AsTask();
      task.Wait();
      item = task.Result;
    }

    if (item.Length > count)
    {
      Array.Copy(item, 0, buffer, offset, count);
      ReadQueue.Append(item, count, item.Length - count);
      return count;
    }
    else
    {
      Array.Copy(item, 0, buffer, 0, item.Length);
      return item.Length;
    }
  }

  public override void Write(byte[] buffer, int offset, int count)
  {
    Channel.Writer.WriteAsync(buffer[offset..(offset + count)]).AsTask().Wait();
  }

  public override void Flush() { }
  public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
  public override void SetLength(long value) => throw new NotSupportedException();
}
