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
using Newtonsoft.Json;

public static partial class Client
{
  public class ClientError(uint responseCode, Exception? innerException = null) : Exception($"Client received server error: {responseCode}", innerException)
  {
    public readonly uint ResponseCode = responseCode;
  }

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

  internal static int State = STATE_NOT_CONNECTED();
  internal static void SetState(int state) => OnStateChange(State = state);
  [JSExport]
  internal static int GetState() => State;

  internal static void Main() { }

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
        ClientWebSocket clientWebSocket = new();

        SetState(STATE_CONNECTING());

        try
        {
          await clientWebSocket.ConnectAsync(new(WS_URL()), CancellationToken.None);
        }
        catch(Exception exception)
        {
          SetState(STATE_NOT_CONNECTED());
          source.SetException(exception);

          throw;
        }

        UserClient client = UserClientWebSocket = new(clientWebSocket, (buffer) => Task.CompletedTask, (payload, cancellationToken) => Task.FromResult<HybridWebSocket.Payload>(new(0, [])));
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

  internal static UserClient? UserClientWebSocket = null;

  [JSExport]
  internal static int? GetRequestInt(string requestString)
  {
    foreach (UserRequest request in Enum.GetValues<UserRequest>())
    {
      if (requestString == request.ToString())
      {
        return (byte)request;
      }
    }

    return null;
  }

  [JSExport]
  internal static string? GetRequestString(int requestInt)
  {
    foreach (UserRequest request in Enum.GetValues<UserRequest>())
    {
      if (requestInt == ((byte)request))
      {
        return request.ToString();
      }
    }

    return null;
  }

  [JSExport]
  internal static int? GetResponseInt(string responseString)
  {
    foreach (UserResponse response in Enum.GetValues<UserResponse>())
    {
      if (responseString == response.ToString())
      {
        return (byte)response;
      }
    }

    return null;
  }

  [JSExport]
  internal static string? GetResponseString(int responseInt)
  {
    foreach (UserResponse response in Enum.GetValues<UserResponse>())
    {
      if (responseInt == ((byte)response))
      {
        return response.ToString();
      }
    }

    return null;
  }

  private static ClientPayload? LastResponsePayload = null;

  [JSExport]
  internal static int? GetResponseType() => (int?)LastResponsePayload?.Type;

  [JSExport]
  internal static byte[] ReceiveRawResponse()
  {
    if (LastResponsePayload == null || LastResponsePayload is not ClientPayload.Raw rawPayload)
    {
      throw new Exception($"Invalid payload type {LastResponsePayload?.Type}.");
    }

    return rawPayload.Bytes.ToByteArray();
  }

  [JSExport]
  internal static string ReceiveJsonResponse()
  {
    if (LastResponsePayload == null || LastResponsePayload is not ClientPayload.JSON jsonPayload)
    {
      throw new Exception($"Invalid payload type {LastResponsePayload?.Type}.");
    }

    return jsonPayload.Json.ToString(Formatting.None);
  }

  [JSExport] internal static Task<int> SendJsonRequest(int requestCode, string requestPayload) => SendRequest(requestCode, new ClientPayload.JSON(JToken.Parse(requestPayload)));
  [JSExport] internal static Task<int> SendRawRequest(int requestCode, byte[] requestPayload) => SendRequest(requestCode, new ClientPayload.Raw(requestPayload));

  internal static async Task<int> SendRequest(int requestCode, ClientPayload requestPayload)
  {
    LastResponsePayload = null;

    if (GetRequestString(requestCode) == null)
    {
      return (int)UserResponse.InvalidCommand;
    }

    (uint responseCode, CompositeBuffer responseBuffer) = await UserClientWebSocket!.Request(new((uint)requestCode, requestPayload.Serialize()), CancellationToken.None);

    if (responseCode == (uint)UserResponse.Okay)
    {
      LastResponsePayload = ClientPayload.Deserialize(responseBuffer);
    }

    return (int)responseCode;
  }
}
