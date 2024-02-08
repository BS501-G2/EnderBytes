using System.Text;

namespace RizzziGit.EnderBytes.Protocols;

using Resources;
using RizzziGit.EnderBytes.Connections;
using Services;

public sealed partial class FtpProtocol
{
  private abstract record Command
  {
    public static Command? Parse(string rawCommand) => Parse(rawCommand.Split(' '));
    public static Command? Parse(string[] rawCommand)
    {
      try
      {
        switch (rawCommand.ElementAt(0))
        {
          case "USER": return new USER(rawCommand.ElementAt(1));
          case "PASS": return new PASS(rawCommand.ElementAt(1));
        }
      }
      catch { }

      return new UNKNOWN(string.Join(' ', rawCommand));
    }

    private Command() { }

    public sealed record USER(string Username) : Command;
    public sealed record PASS(string Password) : Command
    {
      public override string ToString()
      {
        StringBuilder builder = new();

        (this with { Password = "<censored>" }).PrintMembers(builder);

        return $"{GetType().Name} {{ {builder} }}";
      }
    }
    public sealed record UNKNOWN(string RawCommand) : Command;
  }

  public new sealed partial class Connection
  {
    private async Task<Reply> HandleCommand(Command? command, BasicConnection connection, CancellationToken cancellationToken) => command switch
    {
      Command.USER userCommand => await HandleCommand(userCommand, connection, cancellationToken),
      Command.PASS passCommand => await HandleCommand(passCommand, connection, cancellationToken),
      _ => new(502, "Command not implemented."),
    };

    private string? Username;

    private Task<Reply> HandleCommand(Command.USER command, BasicConnection connection, CancellationToken cancellationToken)
    {
      Username = command.Username;

      if (Username == "anonymous")
      {
        return Task.FromResult<Reply>(new(332, "Anonymous not allowed."));
      }

      return Task.FromResult<Reply>(new(330, "Type password."));
    }

    private async Task<Reply> HandleCommand(Command.PASS command, BasicConnection connection, CancellationToken cancellationToken)
    {
      if (Username == null)
      {
        return error();
      }

      UserConfigurationResource? userConfiguration = null;
      UserAuthenticationResource.Token? token = null;

      await Service.Server.ResourceService.Transact((transaction, resourceService, cancellationToken) =>
      {
        UserResource? user = resourceService.Users.GetByUsername(transaction, Username);
        if (user == null)
        {
          return;
        }

        userConfiguration = resourceService.UserConfiguration.Get(transaction, user);
        token = resourceService.UserAuthentications.GetByPayload(transaction, user, Encoding.ASCII.GetBytes(command.Password), UserAuthenticationResource.UserAuthenticationType.Password);
      }, cancellationToken);

      if (userConfiguration == null || token == null)
      {
        return error();
      }

      if (!userConfiguration.EnableFtpAccess)
      {
        return new(534, "FTP access not enabled.");
      }

      Service.Server.SessionService.NewSession(connection, token);
      return new(230, "Logged in.");

      static Reply error() => new(431, "Invalid username or password.");
    }
  }
}
