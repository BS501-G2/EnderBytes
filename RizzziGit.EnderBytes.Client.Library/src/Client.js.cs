using System;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RizzziGit.EnderBytes.Services;

namespace RizzziGit.EnderBytes.Client.Library;

public partial class Client
{
  [JSImport("getUrl", "main.js")]
  internal static partial string GetUrl();

  [JSImport("updateState", "main.js")]
  internal static partial void JsSetstate(int status);

  [JSExport]
  internal static int JsGetState() => (int)State;

  [JSExport]
  internal static void OnConnectionChanged(bool isConnected)
  {
    if (!isConnected)
    {
      try { CancellationTokenSource?.Cancel(); } catch { }
    }
  }

  [JSExport]
  internal static async Task Login(string Username, string Password)
  {
    ClientResponse response = await Request(new LoginRequest(Username, Encoding.UTF8.GetBytes(Password)), CancellationToken.None);

    if (response is ErrorResponse errorResponse)
    {
      errorResponse.Throw();
    }

    _ = (LoginResponse)response;
  }

  [JSExport]
  internal static async Task LoginWithToken(string Username, string Password)
  {
    ClientResponse response = await Request(new LoginRequest(Username, Encoding.UTF8.GetBytes(Password)), CancellationToken.None);

    if (response is ErrorResponse errorResponse)
    {
      errorResponse.Throw();
    }

    _ = (LoginResponse)response;
  }

  internal static async Task<ClientResponse> Request(ClientRequest request, CancellationToken cancellationToken = default)
  {
    TaskCompletionSource<ClientResponse> source = new();

    await RequestQueue.Enqueue(new(request, source), cancellationToken);
    return await source.Task;
  }
}
