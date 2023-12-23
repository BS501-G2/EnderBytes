namespace RizzziGit.EnderBytes.Services;

using Framework.Services;

public sealed partial class ConnectionService
{
  public abstract partial record Configuration
  {
    private Configuration() { }
  }

  public abstract partial class Connection : Lifetime
  {
    private Connection(Configuration configuration) : base("Connection")
    {
      Configuration = configuration;
    }

    public readonly Configuration Configuration;
    public UserService.Session? Session { get; private set; } = null;

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
      try
      {
        await base.OnRun(cancellationToken);
      }
      finally
      {

      }
    }
  }
}
