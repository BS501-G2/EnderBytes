namespace RizzziGit.EnderBytes.Services;

using Resources;

public sealed partial class ConnectionService
{
  public abstract record Parameters(EndPoint RemoteEndPoint, EndPoint LocalEndPoint)
  {
    public sealed record Basic(EndPoint RemoteEndPoint, EndPoint LocalEndPoint) : Parameters(RemoteEndPoint, LocalEndPoint);
    public sealed record Advanced(EndPoint RemoteEndPoint, EndPoint LocalEndPoint) : Parameters(RemoteEndPoint, LocalEndPoint);
    public sealed record Internal(UserAuthentication UserAuthentication, byte[] PayloadHash) : Parameters(new EndPoint.Null(), new EndPoint.Null());
  }
}
