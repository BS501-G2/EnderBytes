namespace RizzziGit.EnderBytes.Connections;

public abstract partial class Connection
{
  public new abstract class Exception : System.Exception
  {
    private Exception(string? message = null, System.Exception? innerException = null) : base(message, innerException) { }

    public sealed class AlreadyLoggedIn() : Exception("Already logged in.");
    public sealed class NotFound() : Exception("Resource does not exist.");
    public sealed class AccessDenied(System.Exception? innerException = null) : Exception("Error trying to gain access.", innerException);
  }
}
