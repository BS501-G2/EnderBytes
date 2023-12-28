namespace RizzziGit.EnderBytes.Services;

public sealed partial class StorageHubService
{
  public abstract partial class Hub
  {
    private sealed partial class Blob(StorageHubService service, long hubId, KeyService.Transformer.Key hubKey) : Hub(service, hubId, hubKey)
    {
      protected override Hub.Session Internal_NewSession(ConnectionService.Connection connection) => new Session(this, connection);
    }
  }
}
