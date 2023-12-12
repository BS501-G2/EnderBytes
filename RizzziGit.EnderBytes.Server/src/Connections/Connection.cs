namespace RizzziGit.EnderBytes.Connections;

using Resources;
using Utilities;
using Database;
using StoragePools;

public enum ConnectionType { Basic, Advanced, Internal }

public abstract partial class Connection : Lifetime
{
  public record SessionInformation(
    UserResource User,
    UserKeyResource.Transformer UserKeyTransformer,
    UserAuthenticationResource UserAuthentication,
    byte[] HashCache
  );

  private Connection(ConnectionManager manager, long id) : base($"#{id}")
  {
    Manager = manager;
    Id = id;
    FileHandles = [];
  }

  public readonly long Id;
  public readonly ConnectionManager Manager;
  public readonly ConnectionType Type;

  public Server Server => Manager.Server;
  public ResourceManager ResourceManager => Server.Resources;
  public Database Database => ResourceManager.Database;

  public SessionInformation? Session { get; private set; }

  private readonly Dictionary<long, StoragePool.INode.IFile.IStream> FileHandles;

  private async Task ClearHandles()
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
        await Database.RunTransaction((transaction) =>
        {
        }, CancellationToken.None);
      }
      catch { }
    }
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    try
    {
      await base.OnRun(cancellationToken);
    }
    catch
    {
      await ClearHandles();
      throw;
    }
  }

  public Task ClearSession() => RunTask(async () =>
  {
    await ClearHandles();
    lock (this)
    {
      Session = null;
    }
  });

  public async Task<SessionInformation> SetSession(UserResource user, UserAuthenticationResource userAuthentication, byte[] hashCache)
  {
    try
    {
      return await RunTask(async (cancellationToken) => await Database.RunTransaction((transaction) =>
      {
        lock (this)
        {
          if (Session != null)
          {
            throw new Exception.AlreadyLoggedIn();
          }

          UserKeyResource? userKey = ResourceManager.UserKeys.GetByUserAuthentication(transaction, user, userAuthentication);
          UserKeyResource.Transformer transformer = userKey!.GetTransformer(hashCache);

          return Session = new(user, transformer, userAuthentication, hashCache);
        }
      }));
    }
    finally
    {
      await ClearHandles();
    }
  }

  public async Task<StoragePool[]> List()
  {
    if (Session == null)
    {
      throw new Exception.AccessDenied();
    }

    List<StoragePoolResource> resources = [];

    await Database.RunTransaction((transaction) =>
    {
      foreach (KeyResource key in ResourceManager.Keys.StreamKeysByUser(transaction, Session.User))
      {
        StoragePoolResource? storagePool = ResourceManager.StoragePools.GetByKeySharedId(transaction, key);
        if (storagePool == null)
        {
          continue;
        }

        resources.Add(storagePool);
      }
    });

    return await Task.WhenAll(resources.Select((storagePool) => Server.StoragePools.Get(storagePool, Session, CancellationToken.None)));
  }
}
