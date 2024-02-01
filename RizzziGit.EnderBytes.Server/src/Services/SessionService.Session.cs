namespace RizzziGit.EnderBytes.Services;

using Resources;

public sealed partial class SessionService
{
  public sealed class Session
  {
    public Session(SessionService service, long id, UserAuthenticationResource userAuthentication, byte[] payloadHash, ConnectionService.Connection connection)
    {
      Service = service;
      Id = id;
      UserAuthentication = userAuthentication;
      PayloadHash = payloadHash;
      Connection = connection;

      userAuthentication.ThrowIfPayloadHashInvalid(PayloadHash);
    }

    public readonly SessionService Service;

    public readonly long Id;
    public readonly UserAuthenticationResource UserAuthentication;

    private readonly byte[] PayloadHash;

    public readonly ConnectionService.Connection Connection;

    public byte[] Encrypt(byte[] bytes) => UserAuthentication.Encrypt(bytes);
    public byte[] Decrypt(byte[] bytes) => UserAuthentication.Decrypt(bytes, PayloadHash);

    public bool IsValid => Service.IsSessionValid(Connection, this);
    public void ThrowIfValid() => Service.ThrowIfSessionInvalid(Connection, this);
  }
}
