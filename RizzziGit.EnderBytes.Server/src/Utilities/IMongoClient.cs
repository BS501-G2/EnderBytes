namespace RizzziGit.EnderBytes.Utilities;

public static class IMongoClientExtensions
{
  public delegate void OnFailure();

  public delegate void Callback(CancellationToken cancellationToken);
  public delegate T Callback<T>(CancellationToken cancellationToken);
}
