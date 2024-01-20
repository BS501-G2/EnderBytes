namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;
using Framework.Lifetime;

public sealed partial class ConnectionService
{
  public abstract partial class Connection : Lifetime
  {
    private Connection(ConnectionService service, Parameters configuration) : base($"Connection")
    {
      Service = service;
      Configuration = configuration;

      Id = service.NextId++;
    }

    public readonly ConnectionService Service;
    public readonly long Id;

    private readonly Parameters Configuration;

    protected override void OnCreate()
    {
      lock (Service)
      {
        Service.Connections.Add(Id, this);
      }
    }
  }

  private long NextId;
  private readonly WeakDictionary<long, Connection> Connections = [];
}
