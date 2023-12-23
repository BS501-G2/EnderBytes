namespace RizzziGit.EnderBytes.Services;

using Framework.Services;

public sealed partial class UserService
{
  public sealed class Session(GlobalSession global, long id, KeyGeneratorService.Transformer.UserAuthentication transformer) : Lifetime($"#{id}", global)
  {
    public readonly GlobalSession Global = global;
    public readonly long Id = id;
    public long UserId => Global.UserId;
    public readonly KeyGeneratorService.Transformer.UserAuthentication Transformer = transformer;

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
