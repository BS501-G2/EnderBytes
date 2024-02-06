namespace RizzziGit.EnderBytes.Connections;

using Services;
using Resources;
using Extras;

public abstract partial class Connection<C, CC, Rq, Rs>(ConnectionService service, CC configuration, long id) : ConnectionService.Connection(service, configuration, id), IDisposable
  where C : Connection<C, CC, Rq, Rs>
  where CC : Connection<C, CC, Rq, Rs>.ConnectionConfiguration
  where Rq : Connection<C, CC, Rq, Rs>.Request
  where Rs : Connection<C, CC, Rq, Rs>.Response
{
  public abstract record Request
  {
    public sealed record Login(string Username, UserAuthenticationResource.UserAuthenticationType AuthenticationType, byte[] AuthenticationPayload);
    public sealed record LoginWithToken(UserAuthenticationResource.Token Token);
    public sealed record Logout();
  }

  public abstract record Response(Response.ResponseStatus Status, string? Message)
  {
    public enum ResponseStatus { OK, Error }

    public sealed record OK() : Response(ResponseStatus.OK, null);

    public sealed record InvalidRequest() : Response(ResponseStatus.Error, "Invalid request.");
    public sealed record InvalidCredentials() : Response(ResponseStatus.Error, "Invalid credentials.");

    public sealed record CurrentSessionExists() : Response(ResponseStatus.Error, "Current session exists.");
    public sealed record NoCurrentSession() : Response(ResponseStatus.Error, "No current session.");
  }

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
    UserResource? user = await Service.Server.ResourceService.Transact(ResourceService.Scope.Main, (transaction, resources, cancellationToken) => resources.Users.GetByUsername(transaction, loginRequest.Username), cancellationToken);

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
