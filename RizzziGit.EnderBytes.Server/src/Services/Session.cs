namespace RizzziGit.EnderBytes.Services.Session;

using Resources;
using Services.Connection;
using Framework.Collections;
using Framework.Lifetime;

public sealed class Session : Lifetime
{
  public sealed class SessionService(Server server) : Server.SubService(server, "Sessions")
  {
    private long NextId;
    private readonly WeakDictionary<Connection, Session> Sessions = [];

    public Session? GetSession(Connection connection, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();

      lock (this)
      {
        cancellationToken.ThrowIfCancellationRequested();

        Sessions.TryGetValue(connection, out Session? session);
        return session;
      }
    }

    public Session CreateSession(Connection connection, User user, UserAuthentication userAuthentication, byte[] payloadHash, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();

      lock (this)
      {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Sessions.TryGetValue(connection, out Session? session))
        {
          long sessionId = NextId++;
          session = new(connection, this, sessionId, user, userAuthentication, payloadHash);

          connection.Stopped += (_, _) => Sessions.Remove(connection);
          Sessions.Add(connection, session);
          session.Start(GetCancellationToken());

          return session;
        }

        throw new InvalidOperationException("Session already exists.");
      }
    }

    public void DestroySession(Connection connection, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();

      lock (this)
      {
        cancellationToken.ThrowIfCancellationRequested();

        Sessions.Remove(connection);
      }
    }
  }

  private Session(Connection connection, SessionService service, long id, User user, UserAuthentication userAuthentication, byte[] payloadHash) : base($"#{id}", service.Logger)
  {
    Service = service;
    Connection = connection;
    Id = id;
    User = user;

    userAuthentication.ThrowIfPayloadHashInvalid(payloadHash);

    UserAuthentication = userAuthentication;
    PayloadHash = payloadHash;
  }

  public readonly long Id;
  public readonly SessionService Service;
  public readonly Connection Connection;

  public readonly User User;
  public readonly UserAuthentication UserAuthentication;
  private readonly byte[] PayloadHash;

  public byte[] Encrypt(byte[] bytes)
  {
    lock (this)
    {
      return UserAuthentication.Encrypt(bytes);
    }
  }

  public byte[] Decrypt(byte[] bytes)
  {
    lock (this)
    {
      return UserAuthentication.Decrypt(bytes, PayloadHash);
    }
  }
}
