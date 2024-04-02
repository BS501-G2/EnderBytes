using System.Net.WebSockets;

namespace RizzziGit.EnderBytes.Services;

using Commons.Memory;
using Commons.Net;

public class UserClientWebSocket(WebSocket webSocket, Func<HybridWebSocket.Payload, CancellationToken, Task<HybridWebSocket.Payload>> onRequest) : HybridWebSocket(new()
{
  MaxWebSocketPerMessageSize = 1024 * 256
}, webSocket)
{
  public const int REQ_PASSWORD_LOGIN = 0;
  public const int REQ_TOKEN_LOGIN = 1;

  public const int REQ_DESTROY_TOKEN = 2;
  public const int REQ_GET_TOKEN = 3;

  public const int REQ_RANDOM_BYTES = 4;

  public const int REQ_FILESYSTEM_GET = 5;
  public const int REQ_FILESYSTEM_TRASH = 6;
  public const int REQ_FILESYSTEM_DELETE = 7;

  public const int REQ_FILESYSTEM_FILE_SNAPSHOT_CREATE = 9;
  public const int REQ_FILESYSTEM_FILE_CREATE = 13;
  public const int REQ_FILESYSTEM_FILE_OPEN = 8;
  public const int REQ_FILESYSTEM_FILE_CLOSE = 10;

  public const int REQ_FILESYSTEM_FOLDER_SCAN = 11;
  public const int REQ_FILESYSTEM_FOLDER_CREATE = 12;

  public const int RES_OK = 0;
  public const int RES_ERR_INVALID_CREDENTIALS = 1;
  public const int RES_ERR_INVALID_COMMAND = 2;
  public const int RES_ERR_LOGIN_REQUIRED = 3;

  protected override Task OnStart(CancellationToken cancellationToken) => Task.CompletedTask;
  protected override Task OnMessage(CompositeBuffer message, CancellationToken cancellationToken) => Task.CompletedTask;
  protected override Task<Payload> OnRequest(Payload payload, CancellationToken cancellationToken) => onRequest(payload, cancellationToken);

  public new Task Start(CancellationToken cancellationToken = default) => base.Start(cancellationToken);
}
