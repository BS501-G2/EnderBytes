using System.Threading.Tasks;
using System;
using System.Net.WebSockets;
using System.Threading;

namespace RizzziGit.EnderBytes.Client.Library;

using Commons.Memory;
using Commons.Net;
using Services;

public partial class Client
{
  public Client(ClientWebSocket webSocket)
  {
    UnderlyingWebSocket = webSocket;

    HybridWebSocket = new(this, new(), UnderlyingWebSocket, (func) => { StartFunc = func; });
  }

  private readonly ClientWebSocket UnderlyingWebSocket;
  private readonly WebSocketClient HybridWebSocket;
  private Func<CancellationToken, Task>? StartFunc = null;

  public async Task<bool> Run(CancellationToken cancellationToken)
  {
    try
    {
      State = ClientState.Open;

      await StartFunc!.Invoke(cancellationToken);

      while (HybridWebSocket.State == ConnectionState.Open)
      {
        CurrentClientRequest ??= await RequestQueue.Dequeue(cancellationToken);

        TaskCompletionSource<ClientResponse> source = CurrentClientRequest!.Source;

        try
        {
          source.SetResult(ClientResponse.Deserialize(await HybridWebSocket.Request(CurrentClientRequest.Request.Serialize(), cancellationToken)));
          CurrentClientRequest = null;
        }
        catch (WebSocketException) { break; }
        catch
        {
          source.SetCanceled(cancellationToken);
        }
      }

      return true;
    }
    finally
    {
      State = ClientState.Closing;
      State = ClientState.Closed;
    }
  }

  private Task HandleMessage(CompositeBuffer message, CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }

  private Task<HybridWebSocket.Payload> HandleRequest(HybridWebSocket.Payload payload, CancellationToken cancellationToken = default)
  {
    return Task.FromResult<HybridWebSocket.Payload>(new(0, []));
  }
}
