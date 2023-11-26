namespace RizzziGit.EnderBytes.StoragePools;

public abstract partial class StoragePool
{
  public abstract partial class Exception : System.Exception
  {
    private Exception(string? message = null, System.Exception? cause = null) : base(message, cause) {}

    public sealed class HandleNotFound() : Exception("Handle with the specified path could not be found.");
  }
}
