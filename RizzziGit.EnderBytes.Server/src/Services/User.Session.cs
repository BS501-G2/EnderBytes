namespace RizzziGit.EnderBytes.Services;

using Utilities;

public sealed partial class UserService
{
  public sealed class Session(GlobalSession global, long id, KeyGeneratorService.Transformer.UserAuthentication transformer) : Lifetime($"#{id}", global)
  {
    public readonly GlobalSession Global = global;
    public readonly long Id = id;
    public readonly KeyGeneratorService.Transformer.UserAuthentication Transformer = transformer;
  }
}
