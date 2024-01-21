namespace RizzziGit.EnderBytes.Services;

using Framework.Lifetime;

using Resources;

using Connection = ConnectionService.Connection;

public sealed partial class StorageService
{
  public sealed partial class Storage
  {
    public sealed partial class Session(Storage storage, Connection connection, UserAuthentication? userAuthentication) : Lifetime
    {
      public readonly Storage Storage = storage;
      public readonly Connection Connection = connection;
      public readonly UserAuthentication? UserAuthentication = userAuthentication;

      public bool IsValid => Connection.Session?.UserAuthentication == UserAuthentication;
    }
  }
}
