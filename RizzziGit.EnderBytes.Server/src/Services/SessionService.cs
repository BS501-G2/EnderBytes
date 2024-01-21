namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;

using Core;
using Resources;

using Connection = ConnectionService.Connection;

public sealed partial class SessionService(Server server) : Server.SubService(server, "Sessions")
{
  private long NextId;
  private readonly WeakDictionary<Connection, Session> Sessions = [];

  public void DestroySession(Session session)
  {
    lock (this)
    {
      Sessions.Remove(session.Connection);
    }
  }

  public bool IsValid(Session session)
  {
    lock (this)
    {
      bool output = session.UserAuthentication.IsValid && Sessions.TryGetValue(session.Connection, out Session? existingSession) && session == existingSession;

      if (!output)
      {
        Sessions.Remove(session.Connection);
      }

      return output;
    }
  }

  public Session? GetSession(Connection connection)
  {
    lock (this)
    {
      Sessions.TryGetValue(connection, out Session? session);
      return session;
    }
  }

  public Session CreateSessionWithPayload(Connection connection, UserAuthentication userAuthentication, byte[] payload) => CreateSessionWithPayloadHash(connection, userAuthentication, userAuthentication.GetPayloadHash(payload));
  public Session CreateSessionWithPayloadHash(Connection connection, UserAuthentication userAuthentication, byte[] payloadHash)
  {
    userAuthentication.ThrowIfPayloadHashInvalid(payloadHash);

    lock (this)
    {
      if (Sessions.TryGetValue(connection, out Session? _))
      {
        throw new InvalidOperationException("Session already exists.");
      }

      Session session = new(this, connection, userAuthentication, payloadHash);
      Sessions.Add(connection, session);
      return session;
    }
  }
}
