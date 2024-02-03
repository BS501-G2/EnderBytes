namespace RizzziGit.EnderBytes.Services;

using Resources;

public sealed partial class SessionService
{
  public sealed class Session
  {
    public Session(SessionService service, long id, UserAuthenticationResource.Token token, ConnectionService.Connection connection)
    {
      Service = service;
      Id = id;
      Token = token;
      Connection = connection;
    }

    public readonly SessionService Service;

    public readonly long Id;
    public readonly UserAuthenticationResource.Token Token;

    public readonly ConnectionService.Connection Connection;

    public byte[] Encrypt(byte[] bytes) => Token.Encrypt(bytes);
    public byte[] Decrypt(byte[] bytes) => Token.Decrypt(bytes);

    public bool IsValid => Service.IsSessionValid(Connection, this);
    public void ThrowIfValid() => Service.ThrowIfSessionInvalid(Connection, this);
  }
}
