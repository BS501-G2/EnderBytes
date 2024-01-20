namespace RizzziGit.EnderBytes.Services;

using Framework.Lifetime;

public sealed partial class ConnectionService
{
  public abstract partial class Connection : Lifetime
  {
    private Connection(ConnectionService service, Parameters parameters)
    {
      Service = service;
      Parameters = parameters;

      Id = Service.NextId++;
    }

    public readonly ConnectionService Service;
    public readonly long Id;

    private readonly Parameters Parameters;

    public virtual bool IsValid => Service.IsValid(this);
    public void ThrowIfInvalid()
    {
      if (!IsValid)
      {
        throw new InvalidOperationException("Connection is invalid.");
      }
    }
  }

  private long NextId;
}
