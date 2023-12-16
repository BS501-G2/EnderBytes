namespace RizzziGit.EnderBytes.Services;

public enum BlobNodeType { File, Folder, SymbolicLink }

public sealed partial class StorageHubService
{
  public abstract partial class Hub
  {
    public sealed class Blob(StorageHubService service, long hubId, KeyGeneratorService.Transformer.Key hubKey) : Hub(service, hubId, hubKey)
    {
    }
  }
}
