namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;

using Core;
using Resources;

public sealed partial class SessionService(Server server) : Server.SubService(server, "Sessions")
{
  private long NextId = 0;
  private readonly WeakDictionary<ConnectionService.Connection, Session> Sessions = [];

  public bool IsValid(ConnectionService.Connection connection, Session session)
  {
    lock (connection)
    {
      lock (session)
      {
        lock (this)
        {
          return Sessions.TryGetValue(connection, out Session? testSession) &&
            testSession == session &&
            session.UserAuthentication.IsValid &&
            session.Connection.IsValid;
        }
      }
    }
  }

  public void ThrowIfInvalid(ConnectionService.Connection connection, Session session)
  {
    if (!IsValid(connection, session))
    {
      throw new InvalidOperationException("Invalid session.");
    }
  }

  public void DestroySession(ConnectionService.Connection connection, Session session)
  {
    lock (connection)
    {
      lock (session)
      {
        lock (this)
        {
          ThrowIfInvalid(connection, session);

          Sessions.Remove(connection);
        }
      }
    }
  }

  public Session? GetSession(ConnectionService.Connection connection)
  {
    lock (connection)
    {
      lock (this)
      {
        if (Sessions.TryGetValue(connection, out Session? session) && session.IsValid)
        {
          return session;
        }

        return null;
      }
    }
  }

  public Session NewSession(ConnectionService.Connection connection, UserAuthenticationResource userAuthentication, byte[] payloadHash)
  {
    lock (connection)
    {
      lock (this)
      {
        if (!Sessions.TryGetValue(connection, out Session? session) || !session.IsValid)
        {
          Sessions.AddOrUpdate(connection, session = new(this, NextId++, userAuthentication, payloadHash, connection));
          return session;
        }

        throw new InvalidOperationException("Current session exists.");
      }
    }
  }

  protected override Task OnStop(Exception? exception = null)
  {
    lock (this)
    {
      Sessions.Clear();

      return base.OnStop(exception);
    }
  }
}
