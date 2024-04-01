using System;
using System.Net.WebSockets;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderBytes.Client.Library;

using Commons.Net;
using Commons.Memory;

using Services;

public static partial class Client
{
  [JSImport("STATE_NOT_CONNECTED", "main.js")]
  internal static partial int STATE_NOT_CONNECTED();
  [JSImport("STATE_CONNECTING", "main.js")]
  internal static partial int STATE_CONNECTING();
  [JSImport("STATE_READY", "main.js")]
  internal static partial int STATE_READY();
  [JSImport("STATE_BORKED", "main.js")]
  internal static partial int STATE_BORKED();

  [JSImport("WS_URL", "main.js")]
  internal static partial string WS_URL();

  [JSImport("OnStateChange", "main.js")]
  internal static partial void OnStateChange(int state);
  [JSImport("OnSessionTokenChange", "main.js")]
  internal static partial void OnSessionChange(string sessionToken);

  internal static int State = STATE_NOT_CONNECTED();
  internal static void SetState(int state) => OnStateChange(State = state);
  [JSExport]
  internal static int GetState() => State;

  internal static Task Main() => Task.CompletedTask;

  [JSExport]
  internal static Task Run()
  {
    SetState(STATE_NOT_CONNECTED());
    TaskCompletionSource source = new();

    _ = Task.Run(run);
    return source.Task;

    async Task run()
    {
      try
      {
        SetState(STATE_CONNECTING());
        ClientWebSocket clientWebSocket = new();
        await clientWebSocket.ConnectAsync(new(WS_URL()), CancellationToken.None);

        UserClientWebSocket client = UserClientWebSocket = new(clientWebSocket, (payload, cancellationToken) => Task.FromResult<HybridWebSocket.Payload>(new(0, [])));
        SetState(STATE_READY());

        source.SetResult();
        await client.Start();
      }
      finally
      {
        SetState(STATE_BORKED());
      }
    }
  }

  [JSExport]
  internal static async Task<bool> AuthenticateByPassword(string username, string password)
  {
    JObject request = new()
    {
      { "username", username },
      { "password", password }
    };

    (uint responseCode, CompositeBuffer responseBuffer) = await UserClientWebSocket!.Request(new(UserClientWebSocket.REQ_PASSWORD_LOGIN, request.ToString()), CancellationToken.None);

    if (responseCode == UserClientWebSocket.RES_OK)
    {
      JObject response = JObject.Parse(responseBuffer.ToString());

      OnSessionChange(response.ToString());

      return true;
    }

    return false;
  }

  [JSExport]
  internal static async Task<bool> AuthenticateByToken(string userId, string token)
  {
    JObject request = new()
    {
      { "userId", Convert.ToInt64(userId) },
      { "token", token }
    };

    (uint responseCode, CompositeBuffer responseBuffer) = await UserClientWebSocket!.Request(new(UserClientWebSocket.REQ_TOKEN_LOGIN, request.ToString()), CancellationToken.None);

    if (responseCode == UserClientWebSocket.RES_OK)
    {
      JObject response = JObject.Parse(responseBuffer.ToString());

      OnSessionChange(response.ToString());

      return true;
    }

    return false;
  }

  [JSExport]
  internal static async Task<bool> DestroyToken()
  {
    (uint responseCode, _) = await UserClientWebSocket!.Request(new(UserClientWebSocket.REQ_DESTROY_TOKEN, []), CancellationToken.None);

    if (responseCode == UserClientWebSocket.RES_OK)
    {
      OnSessionChange("null");

      return true;
    }

    return false;
  }

  [JSExport]
  internal static async Task<string> GetToken()
  {
    (_, CompositeBuffer responseBuffer) = await UserClientWebSocket!.Request(new(UserClientWebSocket.REQ_GET_TOKEN, []), CancellationToken.None);

    return responseBuffer.ToString();
  }

  [JSExport]
  internal static async Task<string> RandomBytes(int length)
  {
    JObject request = new()
    {
      { "length", length }
    };

    (_, CompositeBuffer responseBuffer) = await UserClientWebSocket!.Request(new(UserClientWebSocket.REQ_RANDOM_BYTES, request.ToString()), CancellationToken.None);

    return responseBuffer.ToBase64String();
  }

  internal static UserClientWebSocket? UserClientWebSocket = null;
}
