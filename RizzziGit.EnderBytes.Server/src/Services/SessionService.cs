namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;

using Core;
using Resources;

using Connection = ConnectionService.Connection;

public sealed partial class SessionService(Server server, string name) : Server.SubService(server, name)
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

  public Session CreateSessionWithPayload(Connection connection, UserAuthentication userAuthentication, byte[] payload) => CreateSessionWithPayloadHash(connection, userAuthentication, userAuthentication.GetPayloadHash(payload));
  public Session CreateSessionWithPayloadHash(Connection connection, UserAuthentication userAuthentication, byte[] payloadHash)
  {
    userAuthentication.ThrowIfPayloadHashInvalid(payloadHash);

    lock (this)
    {
      if (!Sessions.TryGetValue(connection, out Session? session))
      {
        Sessions.Add(connection, session = new(this, connection, userAuthentication, payloadHash));
      }

      return session;
    }
  }
}
