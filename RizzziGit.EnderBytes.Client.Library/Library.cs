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
using System.Collections.Generic;

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

  [JSImport("SetState", "main.js")]
  internal static partial void SetState(int state);
  [JSImport("GetState", "main.js")]
  internal static partial int GetState();

  internal static void Main() { }

  [JSExport]
  internal static Task<bool> Run()
  {
    if ((GetState() != STATE_NOT_CONNECTED()) && (GetState() != STATE_BORKED()))
    {
      return Task.FromResult(false);
    }

    SetState(STATE_NOT_CONNECTED());
    TaskCompletionSource<bool> source = new();

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
        catch (Exception exception)
        {
          SetState(STATE_NOT_CONNECTED());
          source.SetException(exception);

          throw;
        }

        UserClient client = UserClientWebSocket = new(clientWebSocket, (buffer) => Task.CompletedTask, (payload, cancellationToken) => Task.FromResult<HybridWebSocket.Payload>(new(0, [])));
        SetState(STATE_READY());

        source.SetResult(true);
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

  private sealed record Response(int Code, ClientPayload Payload);
  private readonly static Dictionary<int, Response> Responses = [];

  [JSExport]
  internal static int GetResponseType(int id)
  {
    if (
      !Responses.TryGetValue(id, out Response? response)
    )
    {
      throw new Exception($"Response with id does not exist.");
    };

    return (int)response.Payload.Type;
  }

  [JSExport]
  internal static int GetResponseCode(int id)
  {
    if (
      !Responses.TryGetValue(id, out Response? response)
    )
    {
      throw new Exception($"Response with id does not exist.");
    };

    return response.Code;
  }

  [JSExport]
  internal static byte[] ReceiveRawResponse(int id)
  {
    if (
      !Responses.TryGetValue(id, out Response? response)
    )
    {
      throw new Exception($"Response with id does not exist.");
    };

    if (
      response.Payload is not ClientPayload.Raw rawPayload
    )
    {
      throw new Exception($"Invalid payload type {response.Payload.Type}.");
    }

    return rawPayload.Bytes.ToByteArray();
  }

  [JSExport]
  internal static string ReceiveJsonResponse(int id)
  {
    if (
      !Responses.TryGetValue(id, out Response? response)
    )
    {
      throw new Exception($"Response with id does not exist.");
    };

    if (
      response.Payload is not ClientPayload.JSON jsonPayload
    )
    {
      throw new Exception($"Invalid payload type {response.Payload.Type}.");
    }

    return jsonPayload.Json.ToString(Formatting.None);
  }

  [JSExport]
  internal static void ClearResponse(int id)
  {
    Responses.Remove(id);
  }

  [JSExport] internal static Task<int> SendJsonRequest(int requestCode, string requestPayload) => SendRequest(requestCode, new ClientPayload.JSON(JToken.Parse(requestPayload)));
  [JSExport] internal static Task<int> SendRawRequest(int requestCode, byte[] requestPayload) => SendRequest(requestCode, new ClientPayload.Raw(requestPayload));

  internal static async Task<int> SendRequest(int requestCode, ClientPayload requestPayload)
  {
    if (GetRequestString(requestCode) == null)
    {
      return (int)UserResponse.InvalidCommand;
    }

    (uint responseCode, CompositeBuffer responseBuffer) = await UserClientWebSocket!.Request(new((uint)requestCode, requestPayload.Serialize()), CancellationToken.None);

    int id;
    do
    {
      id = Random.Shared.Next();
    } while (Responses.ContainsKey(id));

    if (responseCode == (uint)UserResponse.Okay)
    {
      Responses.Add(id, new((int)responseCode, ClientPayload.Deserialize(responseBuffer)));
    }
    else
    {
      Responses.Add(id, new((int)responseCode, new ClientPayload.Raw(responseBuffer)));
    }

    return id;
  }
}
