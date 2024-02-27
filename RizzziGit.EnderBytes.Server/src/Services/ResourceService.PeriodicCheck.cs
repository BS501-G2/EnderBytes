namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;

public sealed partial class ResourceService
{
  public delegate void PeriodicCheckCallback(Transaction transaction, CancellationToken cancellationToken = default);

  public event PeriodicCheckCallback? PeriodicCheck;

  private async Task RunPeriodicCheck(CancellationToken cancellationToken = default)
  {
    while (true)
    {
      if (PeriodicCheck != null)
      {
        await Transact((transaction, cancellationToken) => PeriodicCheck(transaction, cancellationToken), cancellationToken);
      }

      await Task.Delay(5000, cancellationToken);
    }
  }
}
