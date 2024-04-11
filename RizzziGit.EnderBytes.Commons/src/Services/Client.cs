using System.Net.WebSockets;

namespace RizzziGit.EnderBytes.Services;

using Commons.Memory;
using Commons.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class UserClient(
  WebSocket webSocket,
  Func<CompositeBuffer, Task> onMessage,
  Func<HybridWebSocket.Payload, CancellationToken, Task<HybridWebSocket.Payload>> onRequest
) : HybridWebSocket(
  new()
  {
    MaxWebSocketPerMessageSize = (1024 * 32) + 16
  },
  webSocket
)
{
  protected override Task OnStart(CancellationToken cancellationToken) => Task.CompletedTask;
  protected override Task OnMessage(CompositeBuffer message, CancellationToken cancellationToken) => onMessage(message);
  protected override Task<Payload> OnRequest(Payload payload, CancellationToken cancellationToken) => onRequest(payload, cancellationToken);

  public new Task Start(CancellationToken cancellationToken = default) => base.Start(cancellationToken);
}

public abstract record ClientPayload(uint Type, CompositeBuffer Bytes) {
  public static ClientPayload Deserialize(CompositeBuffer source)
  {
    uint type = source.Slice(0, 4).ToUInt32();

    switch (type)
    {
      case 0: return new JSON(JToken.Parse(source.Slice(4).ToString()));
      case 1: return new Raw(source.Slice(4));

      default: throw new ArgumentException("Invalid source.", nameof(source));
    }
  }

  public sealed record JSON(JToken Json) : ClientPayload(0, Json.ToString(Formatting.None))
  {
    public T AsJson<T>() where T : JToken { return (T)Json; }
  }
  public sealed record Raw(CompositeBuffer Buffer) : ClientPayload(1, Buffer);

  public CompositeBuffer Serialize() => CompositeBuffer.Concat(Type, Bytes);
}

public enum UserRequest : uint
{
  Echo,

  LoginToken, LoginPassword, Register, Logout,

  ResolveUserId,

  GetOwnStorage, GetUser, GetFile, GetRootFolderId, ScanFolder, Create, OpenFile, WriteFileBuffer, ReadFileBuffer
}

public enum UserResponse : uint
{
  Okay, LoginRequired, AlreadyLoggedIn, InvalidCredentials,

  ResourceNotFound,

  InvalidCommand, InvalidFormat, UnknownError
}
