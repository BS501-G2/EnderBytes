namespace RizzziGit.EnderBytes.Connections;

using Resources;
using Utilities;
using Database;
using StoragePools;

public enum ConnectionType { Basic, Advanced, Internal }

public abstract partial class Connection : Lifetime
{
  private Connection(ConnectionManager manager, long id) : base($"#{id}")
  {
    Manager = manager;
    Id = id;
  }

  public record MainHubInformation(
    HubResource Hub,
    KeyResource.Transformer Transformer
  );

  public record SessionInformation(
    UserResource User,
    UserKeyResource.Transformer Transformer,
    UserAuthenticationResource UserAuthentication,
    MainHubInformation Information,
    byte[] HashCache
  );

  public new abstract class Exception : System.Exception
  {
    private Exception(string? message = null, System.Exception? innerException = null) : base(message, innerException) { }

    public sealed class SessionException() : Exception("Already logged in.");
    public sealed class SessionError() : Exception();
  }

  public readonly long Id;
  public readonly ConnectionManager Manager;
  public readonly ConnectionType Type;

  public Server Server => Manager.Server;
  public ResourceManager ResourceManager => Server.Resources;
  public Database Database => ResourceManager.Database;

  public SessionInformation? Session { get; private set; }

  private StoragePool.Path CurrentPath = [];

  public Task ClearSession() => RunTask(() =>
  {
    lock (this)
    {
      Session = null;
    }
  });

  public Task<SessionInformation> SetSession(UserResource user, UserAuthenticationResource userAuthentication, byte[] hashCache) => RunTask(async (cancellationToken) => await Database.RunTransaction((transaction) =>
  {
    lock (this)
    {
      if (Session != null)
      {
        throw new Exception.SessionException();
      }

      UserKeyResource? userKey = ResourceManager.UserKeys.GetByUserAuthentication(transaction, user, userAuthentication);
      UserKeyResource.Transformer transformer = userKey!.GetTransformer(hashCache);
      HubResource? hub = ResourceManager.Hubs.GetMainByUserId(transaction, user);
      KeyResource key;

      if (hub == null)
      {
        var (privateKey, publicKey) = Server.KeyGenerator.GetNew();

        key = ResourceManager.Keys.Create(transaction, transformer, privateKey, publicKey);
        hub = ResourceManager.Hubs.Create(transaction, user, key, HubFlags.PersonalMain);
      }
      else
      {
        key = ResourceManager.Keys.GetBySharedId(transaction, Id, transformer.UserKey.Id)!;
      }

      return Session = new(user, transformer, userAuthentication, new(hub, key.GetTransformer(transformer)), hashCache);
    }
  }));

  public Task<StoragePool.Path> GetDirectory() => RunTask(() => CurrentPath);
  public Task SetDirectory(StoragePool.Path path) => RunTask(async (cancellationToken) =>
  {
    if (path.Length == 0)
    {
      CurrentPath = [];
    }
  });
}
