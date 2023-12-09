namespace RizzziGit.EnderBytes.Connections;

using Resources;
using Utilities;
using Database;
using StoragePools;
using System.Threading;

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
    MainHubInformation Hub,
    byte[] HashCache
  );

  public record CurrentPathInformation(
    StoragePool Pool,
    StoragePool.Path Path,
    StoragePool.INode.IFolder Folder
  );

  public new abstract class Exception : System.Exception
  {
    private Exception(string? message = null, System.Exception? innerException = null) : base(message, innerException) { }

    public sealed class AlreadyLoggedIn() : Exception("Already logged in.");
    public sealed class NotFound() : Exception("Resource does not exist.");
    public sealed class AccessDenied(System.Exception? innerException = null) : Exception("Error trying to gain access.", innerException);
  }

  public readonly long Id;
  public readonly ConnectionManager Manager;
  public readonly ConnectionType Type;

  public Server Server => Manager.Server;
  public ResourceManager ResourceManager => Server.Resources;
  public Database Database => ResourceManager.Database;

  public SessionInformation? Session { get; private set; }

  private CurrentPathInformation? CurrentPath = null;
  private readonly Dictionary<long, StoragePool.INode.IFile.IStream> FileHandles = [];

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    try
    {
      await base.OnRun(cancellationToken);
    }
    catch
    {
      for (int index = 0; index < FileHandles.Count; index++)
      {
        long handleId;
        StoragePool.INode.IFile.IStream handle;

        lock (this)
        {
          (handleId, handle) = FileHandles.ElementAt(index);
          FileHandles.Remove(handleId);
        }

        try
        {
          KeyResource.Transformer transformer = await Database.RunTransaction((transaction) =>
          {
            KeyResource key = ResourceManager.Keys.GetBySharedId(transaction, handle.Pool.Resource.KeySharedId, Session?.Transformer.UserKey?.SharedId)!;
            return key.GetTransformer(Session?.Transformer);
          }, CancellationToken.None);

          await handle.Close(transformer);
        }
        catch { }
      }
      throw;
    }
  }

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
        throw new Exception.AlreadyLoggedIn();
      }

      UserKeyResource? userKey = ResourceManager.UserKeys.GetByUserAuthentication(transaction, user, userAuthentication);
      UserKeyResource.Transformer transformer = userKey!.GetTransformer(hashCache);
      HubResource? hub = ResourceManager.Hubs.GetMainByUserId(transaction, user);
      KeyResource? key;

      if (hub == null)
      {
        var (privateKey, publicKey) = Server.KeyGenerator.GetNew();

        key = ResourceManager.Keys.Create(transaction, transformer, privateKey, publicKey);
        hub = ResourceManager.Hubs.Create(transaction, user, key, HubFlags.PersonalMain);
      }
      else
      {
        key = ResourceManager.Keys.GetBySharedId(transaction, Id, transformer.UserKey.Id);
      }

      if (key == null)
      {
        throw new Exception.AccessDenied();
      }

      return Session = new(user, transformer, userAuthentication, new(hub, key.GetTransformer(transformer)), hashCache);
    }
  }));

  public Task<StoragePool.Path> GetDirectory() => RunTask(() => CurrentPath?.Path ?? []);
  public Task SetDirectory(StoragePool.Path path) => RunTask(async (cancellationToken) =>
  {
    var (pool, node) = await ResolveHandle(path);
    return CurrentPath = new(pool, path, (StoragePool.INode.IFolder)node);
  });

  private async Task<(StoragePool pool, StoragePool.INode node)> ResolveHandle(StoragePool.Path path)
  {
    var (resource, key, requestPath) = await Database.RunTransaction((transaction) =>
    {
      return Session != null ? resolveHandleWithSession(Session) : resolveHandle();

      (StoragePoolResource resource, KeyResource key, StoragePool.Path requestPath) resolveHandle()
      {
        throw new NotImplementedException();
      }

      (StoragePoolResource resource, KeyResource key, StoragePool.Path requestPath) resolveHandleWithSession(SessionInformation session)
      {
        List<HubEntryResource> hubEntries = new(ResourceManager.HubEntries.Stream(transaction, session.Hub.Hub));

        HubEntryResource? rootHubEntry;
        if ((rootHubEntry = hubEntries.Find((entry) => entry.Path.Length == 0)) == null)
        {
          var (privateKey, publickey) = Server.KeyGenerator.GetNew();

          KeyResource key = ResourceManager.Keys.Create(transaction, session.Transformer, privateKey, publickey);
          StoragePoolResource pool = ResourceManager.StoragePools.CreateBlob(transaction, key, session.User, StoragePoolFlags.None);
          HubEntryResource hubEntry = ResourceManager.HubEntries.Create(transaction, session.Hub.Hub, pool, []);

          hubEntries.Add(rootHubEntry = hubEntry);
        }

        StoragePoolResource? requestResource;
        KeyResource? requestKey;
        StoragePool.Path requestPath;

        hubEntries.Sort((a, b) => b.Path.Length - a.Path.Length);
        foreach (HubEntryResource hubEntry in hubEntries)
        {
          if (!path.IsInsideOfOrEquals(hubEntry.Path))
          {
            continue;
          }

          requestResource = ResourceManager.StoragePools.GetById(transaction, hubEntry.StoragePoolId);
          requestPath = new(path.Skip(hubEntry.Path.Length));

          return result();
        }

        requestResource = ResourceManager.StoragePools.GetById(transaction, rootHubEntry.StoragePoolId);
        requestPath = path;
        return result();

        (StoragePoolResource resource, KeyResource key, StoragePool.Path requestPath) result()
        {
          if (requestResource == null)
          {
            throw new Exception.NotFound();
          }
          else if ((requestKey = ResourceManager.Keys.GetBySharedId(transaction, requestResource.KeySharedId, session.Transformer.UserKey.SharedId)) == null)
          {
            throw new Exception.AccessDenied();
          }

          return (requestResource, requestKey, requestPath);
        }
      }
    });

    KeyResource.Transformer transformer = key.GetTransformer(Session?.Transformer);
    StoragePool pool = await Server.StoragePools.Get(resource, Session, CancellationToken.None);
    StoragePool.INode.IFolder folder = await pool.GetRootFolder(transformer);
    return (pool, (await folder.GetByPath(transformer, requestPath))!);
  }
}
