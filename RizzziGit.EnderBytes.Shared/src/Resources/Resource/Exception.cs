namespace RizzziGit.EnderBytes.Shared.Resources;

public abstract partial class Resource<M, D, R>
{
  public class Exception(int code, string? message = null, Exception? innerException = null) : System.Exception(message, innerException)
  {
    public readonly int Code = code;
  }

  public class ResourceManagerException(int code, string? message = null, Exception? innerException = null) : Exception(code, message, innerException);
  public class CreateResourceException(int code, string? message = null, Exception? innerException = null) : ResourceManagerException(code, message, innerException);
}
