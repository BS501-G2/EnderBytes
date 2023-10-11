namespace RizzziGit.EnderBytes.Connections;

public abstract record ConnectionCommand()
{
  public record AuthenticateWithPassword(string Username, string Password) : ConnectionCommand();
  public record Deauthenticate() : ConnectionCommand();
}
