namespace RizzziGit.EnderBytes.StoragePools;

public abstract partial class StoragePool
{
  public abstract partial class Exception : System.Exception
  {
    private Exception(string? message = null, System.Exception? cause = null) : base(message, cause) {}

    public sealed class InvalidHandleType() : Exception("Invalid handle type.");

    public sealed class NotAFolder() : Exception("Must be a folder.");
    public sealed class NotAFile() : Exception("Must be a file.");

    public sealed class Busy() : Exception("Resource is busy.");
  }
}
