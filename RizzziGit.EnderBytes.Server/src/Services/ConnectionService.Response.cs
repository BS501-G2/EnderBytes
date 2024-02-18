namespace RizzziGit.EnderBytes.Services;

public sealed partial class ConnectionService
{
  public abstract record Response(Response.ResponseStatus Status, string? Message)
  {
    public enum ResponseStatus { OK, Error }

    public sealed record OK() : Response(ResponseStatus.OK, null);

    public sealed record InvalidRequest() : Response(ResponseStatus.Error, "Invalid request.");
    public sealed record InvalidCredentials() : Response(ResponseStatus.Error, "Invalid credentials.");

    public sealed record CurrentSessionExists() : Response(ResponseStatus.Error, "Current session exists.");
    public sealed record NoCurrentSession() : Response(ResponseStatus.Error, "No current session.");
  }
}
