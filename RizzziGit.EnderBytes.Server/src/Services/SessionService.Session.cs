namespace RizzziGit.EnderBytes.Services;

using Framework.Lifetime;

using Resources;

using Connection = ConnectionService.Connection;

public sealed partial class SessionService
{
  public sealed partial class Session(SessionService service, Connection connection, UserAuthentication userAuthentication, byte[] payloadHash) : Lifetime
  {
    public readonly SessionService Service = service;
    public readonly Connection Connection = connection;
    public readonly UserAuthentication UserAuthentication = userAuthentication;
    public readonly long Id = service.NextId++;

    private readonly byte[] PayloadHash = payloadHash;

    public bool IsValid => Service.IsValid(this);
    public void ThrowIfInvalid()
    {
      lock (this)
      {
        if (!IsValid)
        {
          throw new InvalidOperationException("Session is invalid.");
        }
      }
    }

    public byte[] Encrypt(byte[] input)
    {
      ThrowIfInvalid();
      return UserAuthentication.Encrypt(input);
    }

    public byte[] Decrypt(byte[] input)
    {
      ThrowIfInvalid();
      return UserAuthentication.Decrypt(input, PayloadHash);
    }
  }
}
