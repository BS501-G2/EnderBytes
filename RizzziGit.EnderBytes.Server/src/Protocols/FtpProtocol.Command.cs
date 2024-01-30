using System.Text;

namespace RizzziGit.EnderBytes.Protocols;

using Resources;
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
    private async Task<Reply> HandleCommand(Command? command, CancellationToken cancellationToken) => command switch
    {
      Command.USER userCommand => await HandleCommand(userCommand, cancellationToken),
      Command.PASS passCommand => await HandleCommand(passCommand, cancellationToken),
      _ => new(502, "Command not implemented."),
    };

    private string? Username;

    private Task<Reply> HandleCommand(Command.USER command, CancellationToken cancellationToken)
    {
      Username = command.Username;

      if (Username == "anonymous")
      {
        return Task.FromResult<Reply>(new(332, "Anonymous not allowed."));
      }

      return Task.FromResult<Reply>(new(330, "Type password."));
    }

    private async Task<Reply> HandleCommand(Command.PASS command, CancellationToken cancellationToken)
    {
      if (Username == null)
      {
        return error();
      }

      ResourceService resourceService = Service.Server.ResourceService;

      UserConfigurationResource? userConfiguration = null;
      UserAuthenticationResource.Pair? userAuthentication = null;
      await resourceService.Transact(ResourceService.Scope.Main, (transaction, cancellationToken) =>
      {
        UserResource? user = resourceService.Users.GetByUsername(transaction, Username);
        if (user == null)
        {
          return;
        }

        userConfiguration = resourceService.UserConfiguration.Get(transaction, user);
        userAuthentication = resourceService.UserAuthentications.GetByPayload(transaction, user, Encoding.ASCII.GetBytes(command.Password));
      }, cancellationToken);

      if (userConfiguration == null || userAuthentication == null)
      {
        return error();
      }

      if (!userConfiguration.EnableFtpAccess)
      {
        return new(534, "FTP access not enabled.");
      }

      Service.Server.SessionService.NewSession(UnderlyingConnection!, userAuthentication.UserAuthentication, userAuthentication.PayloadHash);
      return new(230, "Logged in.");

      static Reply error() => new(431, "Invalid username or password.");
    }
  }
}
