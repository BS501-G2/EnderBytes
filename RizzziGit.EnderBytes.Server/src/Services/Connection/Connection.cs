namespace RizzziGit.EnderBytes.Services;

using Framework.Services;
using Records;

public sealed partial class ConnectionService
{
  public abstract partial record Configuration
  {
    private Configuration() { }
  }

  public abstract partial class Connection : Lifetime
  {
    private Connection(ConnectionService service, Configuration configuration) : base("Connection")
    {
      Service = service;
      Configuration = configuration;
      Id = Service.NextConnectionId++;
    }

    public readonly long Id;
    public readonly ConnectionService Service;
    public readonly Configuration Configuration;

    public Server Server => Service.Server;
    public UserService.Session? Session { get; private set; } = null;

    public Task<StorageService.Storage.Session> GetStorage(Record.Storage storage, CancellationToken cancellationToken) => RunTask((cancellationToken) => Server.StorageService.GetStorage(storage, this, cancellationToken), cancellationToken);
  }
}
