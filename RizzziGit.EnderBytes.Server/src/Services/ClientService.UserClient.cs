using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderBytes.Services;

using Commons.Memory;
using Commons.Net;
using Newtonsoft.Json;
using Resources;

public sealed partial class ClientService
{
  public sealed partial class UserClient : Client
  {
    public UserClient(ClientService service) : base(service)
    {
    }

    public async Task Handle(WebSocket webSocket, CancellationToken cancellationToken = default)
    {
      UserClientWebSocket clientWebSocket = new(webSocket, (payload, cancellationToken) => Service.Server.ResourceService.Transact((transaction, cancellationToken) =>
      {
        if (UserAuthenticationToken != null)
        {
          // transaction.GetManager<UserAuthenticationSessionTokenResource.ResourceManager>()
        }

        return payload.Code switch
        {
          UserClientWebSocket.REQ_PASSWORD_LOGIN => AuthenticateByPassword(transaction, payload.Buffer, cancellationToken),
          UserClientWebSocket.REQ_TOKEN_LOGIN => AuthenticateByToken(transaction, payload.Buffer, cancellationToken),
          UserClientWebSocket.REQ_DESTROY_TOKEN => DestroyToken(transaction, cancellationToken),
          UserClientWebSocket.REQ_GET_TOKEN => GetToken(),
          UserClientWebSocket.REQ_RANDOM_BYTES => RandomBytes(payload.Buffer),

          _ => new(UserClientWebSocket.RES_ERR_INVALID_COMMAND, []),
        };

      }, cancellationToken));

      await clientWebSocket.Start(cancellationToken);
    }

    public HybridWebSocket.Payload AuthenticateByToken(ResourceService.Transaction transaction, CompositeBuffer bytes, CancellationToken cancellationToken = default)
    {
      JObject request = JObject.Parse(bytes.ToString());

      long userId = (long)request.GetValue("userId")!;
      string token = (string)request.GetValue("token")!;

      if (
        !transaction.GetManager<UserResource.ResourceManager>().TryGetById(transaction, userId, out UserResource? user, cancellationToken) ||
        !transaction.GetManager<UserAuthenticationResource.ResourceManager>().TryGetSessionToken(transaction, user, token, out UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken, cancellationToken)
      )
      {
        return new(UserClientWebSocket.RES_ERR_INVALID_CREDENTIALS, "null");
      }

      UserAuthenticationToken = userAuthenticationToken;

      return new(UserClientWebSocket.RES_OK, new JObject{
        { "userId", userId },
        { "token", token }
      }.ToString(Formatting.None));
    }

    public HybridWebSocket.Payload AuthenticateByPassword(ResourceService.Transaction transaction, CompositeBuffer bytes, CancellationToken cancellationToken = default)
    {
      JObject request = JObject.Parse(bytes.ToString());

      string username = (string)request.GetValue("username")!;
      byte[] password = Encoding.UTF8.GetBytes((string)request.GetValue("password")!);

      if (
        !transaction.GetManager<UserResource.ResourceManager>().TryGetByUsername(transaction, username, out UserResource? user, cancellationToken) ||
        !transaction.GetManager<UserAuthenticationResource.ResourceManager>().TryGetByPayload(transaction, user, password, UserAuthenticationResource.UserAuthenticationType.Password, out UserAuthenticationResource.UserAuthenticationToken? baseUserAuthenticationToken)
      )
      {
        return new(UserClientWebSocket.RES_ERR_INVALID_CREDENTIALS, "null");
      }

      string sessionToken = transaction.GetManager<UserAuthenticationResource.ResourceManager>().CreateSessionToken(transaction, user, baseUserAuthenticationToken);

      if (!transaction.GetManager<UserAuthenticationResource.ResourceManager>().TryGetSessionToken(transaction, user, sessionToken, out UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken, cancellationToken))
      {
        return new(UserClientWebSocket.RES_ERR_INVALID_CREDENTIALS, "null");
      }

      UserAuthenticationToken = userAuthenticationToken;

      return new(UserClientWebSocket.RES_OK, new JObject{
        { "userId", user.Id },
        { "token", sessionToken }
      }.ToString(Formatting.None));
    }

    public HybridWebSocket.Payload DestroyToken(ResourceService.Transaction transaction, CancellationToken cancellationToken = default)
    {
      if (UserAuthenticationToken != null)
      {
        transaction.GetManager<UserAuthenticationResource.ResourceManager>().Delete(transaction, UserAuthenticationToken.UserAuthentication, CancellationToken.None);
        UserAuthenticationToken = null;
      }

      return new(UserClientWebSocket.RES_OK, "null");
    }

    public HybridWebSocket.Payload GetToken()
    {
      if (UserAuthenticationToken != null && UserAuthenticationToken.IsValid)
      {
        long userId = UserAuthenticationToken.UserId;
        string token = Convert.ToHexString(UserAuthenticationToken.PayloadHash);

        return new(UserClientWebSocket.RES_OK, new JObject
        {
          { "userId", userId },
          { "token", token }
        }.ToString(Formatting.None));
      }

      return new(UserClientWebSocket.RES_OK, "null");
    }

    public HybridWebSocket.Payload RandomBytes(CompositeBuffer requestBuffer)
    {
      JObject request = JObject.Parse(requestBuffer.ToString());

      int length = (int)request.GetValue("length")!;

      return new(UserClientWebSocket.RES_OK, CompositeBuffer.Random(length));
    }
  }
}
