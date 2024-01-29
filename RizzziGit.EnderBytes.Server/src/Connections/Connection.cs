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
    public sealed record Logout();
  }

  public record Response(int Code, string? Message);

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
}
