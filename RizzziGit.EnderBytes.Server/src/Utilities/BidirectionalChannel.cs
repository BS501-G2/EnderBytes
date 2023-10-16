using System.Threading.Channels;

namespace RizzziGit.EnderBytes.Resources;

public sealed class BidirectionalChannel<T>
{
  private BidirectionalChannel(Channel<T> channel1, Channel<T> channel2)
  {
    Channel1 = channel1;
    Channel2 = channel2;
  }

  private readonly Channel<T> Channel1;
  private readonly Channel<T> Channel2;

  public ChannelReader<T> Reader => Channel1.Reader;
  public ChannelWriter<T> Writer => Channel2.Writer;

  public void Complete()
  {
    List<Exception> exceptions = [];

    try { Channel1.Writer.Complete(); } catch (Exception exception) { exceptions.Add(exception); }
    try { Channel2.Writer.Complete(); } catch (Exception exception) { exceptions.Add(exception); }

    if (exceptions.Count != 0)
    {
      throw new AggregateException(exceptions);
    }
  }

  public bool TryComplete()
  {
    bool result1 = Channel1.Writer.TryComplete();
    bool result2 = Channel2.Writer.TryComplete();

    return result1 && result2;
  }

  public static (BidirectionalChannel<T> channel1, BidirectionalChannel<T> channel2) CreateBounded(int capacity) => CreateBounded(new BoundedChannelOptions(capacity));
  public static (BidirectionalChannel<T> channel1, BidirectionalChannel<T> channel2) CreateBounded(BoundedChannelOptions boundedChannelOptions)
  {
    Channel<T> channel1 = Channel.CreateBounded<T>(boundedChannelOptions);
    Channel<T> channel2 = Channel.CreateBounded<T>(boundedChannelOptions);

    return (
      new(channel1, channel2),
      new(channel2, channel1)
    );
  }

  public static (BidirectionalChannel<T> channel1, BidirectionalChannel<T> channel2) CreateUnbounded(UnboundedChannelOptions? unboundedChannelOptions = null)
  {
    Channel<T> channel1 = Channel.CreateUnbounded<T>(unboundedChannelOptions ?? new());
    Channel<T> channel2 = Channel.CreateUnbounded<T>(unboundedChannelOptions ?? new());

    return (
      new(channel1, channel2),
      new(channel2, channel1)
    );
  }
}
