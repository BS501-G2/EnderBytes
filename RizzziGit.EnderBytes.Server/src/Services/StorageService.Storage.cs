namespace RizzziGit.EnderBytes.Services;

using Framework.Lifetime;
using Framework.Collections;

using StorageResource = Resources.Storage;
using Connection = ConnectionService.Connection;

public sealed partial class StorageService
{
  public sealed partial class Storage(StorageService service, StorageResource resource) : Lifetime
  {
    public readonly StorageService Service = service;
    public readonly StorageResource Resource = resource;

    public bool IsValid => Resource.IsValid;
    public bool ThrowIfInvalid => Resource.IsValid;

    private readonly WeakDictionary<Connection, Session> Sessions = [];

    public Session? GetSession(Connection connection)
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

    public Session NewSession(Connection connection)
    {
      lock (this)
      {
        if (!Sessions.TryGetValue(connection, out Session? session)  || !session.IsValid)
        {
          session = new(this, connection, connection.Session?.UserAuthentication);
          Sessions.AddOrUpdate(connection, session);
        }

        return session;
      }
    }
  }
}
