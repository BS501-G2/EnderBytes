namespace RizzziGit.EnderBytes;

using Resources;

public struct EnderBytesConfig()
{
  public int DefaultPasswordIterations = 100000;
}

public sealed partial class EnderBytesServer
{
  public EnderBytesServer(EnderBytesConfig? config = null)
  {
    Logger = new("Server");
    Resources = new(this);
    Config = config ?? new();
  }

  public readonly MainResourceManager Resources;

  public readonly EnderBytesLogger Logger;
  public readonly EnderBytesConfig Config;

  public Task RunTransaction(Database.Database.TransactionCallback callback, CancellationToken cancellationToken) => Resources.RunTransaction(callback, cancellationToken);
  public Task<T> RunTransaction<T>(Database.Database.TransactionCallback<T> callback, CancellationToken cancellationToken) => Resources.RunTransaction(callback, cancellationToken);

  public UserResource.ResourceManager Users => Resources.Users;
  public UserAuthenticationResource.ResourceManager UserAuthentications => Resources.UserAuthentications;
  public GuildResource.ResourceManager Guilds => Resources.Guilds;

  public async Task Init(CancellationToken cancellationToken)
  {
    await Resources.Init(cancellationToken);
  }
}
