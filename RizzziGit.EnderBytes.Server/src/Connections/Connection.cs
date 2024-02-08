namespace RizzziGit.EnderBytes.Connections;

using Services;
using Resources;
using Extras;

using Request = Services.ConnectionService.Request;
using Response = Services.ConnectionService.Response;

public abstract partial class Connection<C, CC>(ConnectionService service, CC configuration, long id) : ConnectionService.Connection(service, configuration, id), IDisposable
  where C : Connection<C, CC>
  where CC : Connection<C, CC>.ConnectionConfiguration
{
  public abstract partial record ConnectionConfiguration(
    ConnectionEndPoint RemoteEndPoint,
    ConnectionEndPoint LocalEndPoint
  ) : ConnectionService.ConnectionConfiguration(RemoteEndPoint, LocalEndPoint);

  ~Connection() => Dispose();
  public void Dispose()
  {
    Close();
    GC.SuppressFinalize(this);
  }

  public void Close() => Service.Close(Id, this);

  public virtual Task<Response> ExecuteRequest(Request request, CancellationToken cancellationToken = default) => request switch
  {
    Request.Login loginRequest => HandleRequest(loginRequest, cancellationToken),
    Request.LoginWithToken loginRequest => HandleRequest(loginRequest, cancellationToken),
    Request.Logout loginRequest => HandleRequest(loginRequest, cancellationToken),
    _ => Task.FromResult<Response>(new Response.InvalidRequest()),
  };

  private async Task<Response> HandleRequest(Request.Login loginRequest, CancellationToken cancellationToken)
  {
    UserResource? user = await Service.Server.ResourceService.Transact((transaction, resources, cancellationToken) => resources.Users.GetByUsername(transaction, loginRequest.Username), cancellationToken);

    if (user == null)
    {
      return new Response.InvalidCredentials();
    }

    return new Response.OK();
  }

  private Task<Response> HandleRequest(Request.LoginWithToken loginRequest, CancellationToken cancellationToken)
  {
    if (Session != null)
    {
      return Task.FromResult<Response>(new Response.CurrentSessionExists());
    }

    Authenticate(loginRequest.Token);
    return Task.FromResult<Response>(new Response.OK());
  }

  private Task<Response> HandleRequest(Request.Logout logoutRequest, CancellationToken cancellationToken)
  {
    if (Session == null)
    {
      return Task.FromResult<Response>(new Response.NoCurrentSession());
    }


    Service.Server.SessionService.DestroySession(this, Session);
    return Task.FromResult<Response>(new Response.OK());
  }
}
