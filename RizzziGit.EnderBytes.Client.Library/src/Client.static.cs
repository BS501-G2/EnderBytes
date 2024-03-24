using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;

namespace RizzziGit.EnderBytes.Client.Library;

using Commons.Collections;
using Services;

public partial class Client
{
  public sealed record RequestQueueEntry(ClientRequest Request, TaskCompletionSource<ClientResponse> Source);

  private static readonly WaitQueue<RequestQueueEntry> RequestQueue = new();
  private static CancellationTokenSource? CancellationTokenSource;
  private static RequestQueueEntry? CurrentClientRequest = null;

  public static async Task Main()
  {
    bool autoReconnect = true;

    try
    {
      while (autoReconnect)
      {
        State = ClientState.Opening;

        try
        {
          using CancellationTokenSource cancellationTokenSource = CancellationTokenSource = new();

          ClientWebSocket webSocket = new();
          Client client = new(webSocket);

          await webSocket.ConnectAsync(new(GetUrl()), cancellationTokenSource.Token);
          await client.Run(cancellationTokenSource.Token);
        }
        finally
        {
          CancellationTokenSource = null;
        }
      }
    }
    finally
    {
      State = ClientState.Borked;
    }
  }
}
