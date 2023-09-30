namespace RizzziGit.EnderBytes.Shared.Resources;

public abstract partial class Resource<M, D, R>
{
  public abstract partial class ResourceManager
  {
    protected class Exception(string? message = null, Exception? innerException = null) : System.Exception(message, innerException);
  }
}
