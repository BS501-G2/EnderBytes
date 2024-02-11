namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;

using Core;
using Resources;

public sealed partial class SessionService(Server server) : Server.SubService(server, "Sessions")
{
  private long NextId = 0;
  private readonly WeakDictionary<ConnectionService.Connection, Session> Sessions = [];

  public bool IsSessionValid(ConnectionService.Connection connection, Session session)
  {
    lock (this)
    {
      lock (connection)
      {
        lock (session)
        {
          return connection.IsValid &&
            Sessions.TryGetValue(connection, out Session? testSession) &&
            testSession == session &&
            session.Token.IsValid &&
            session.Connection.IsValid;
        }
      }
    }
  }

  public void ThrowIfSessionInvalid(ConnectionService.Connection connection, Session session)
  {
    if (!IsSessionValid(connection, session))
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
          ThrowIfSessionInvalid(connection, session);

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
        if (Sessions.TryGetValue(connection, out Session? session))
        {
          return session;
        }

        return null;
      }
    }
  }

  public Session NewSession(ConnectionService.Connection connection, UserAuthenticationResource.Token token)
  {
    lock (connection)
    {
      connection.ThrowIfInvalid();

      lock (this)
      {
        if (!Sessions.TryGetValue(connection, out Session? session))
        {
          Sessions.AddOrUpdate(connection, session = new(this, NextId++, token, connection));
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
