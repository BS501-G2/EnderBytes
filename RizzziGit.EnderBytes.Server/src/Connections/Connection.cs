namespace RizzziGit.EnderBytes.Connections;

using Resources;
using Utilities;
using Database;

public enum ConnectionType { Basic, Advanced, Internal }

public abstract partial class Connection : Lifetime
{
  private Connection(ConnectionManager manager, long id) : base($"#{id}")
  {
    Manager = manager;
    Id = id;
  }

  public record SessionInformation(UserResource User, UserKeyResource.Transformer Transformer, UserAuthenticationResource UserAuthentication, byte[] HashCache);

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

  public Task ClearSession() => RunTask(() =>
  {
    Session = null;
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

      return Session = new(user, transformer, userAuthentication, hashCache);
    }
  }));
}
