using System.Text;

namespace RizzziGit.EnderBytes.Connections;

using Resources;
using Utilities;
using Sessions;
using Extensions;

public abstract class Connection : Lifetime
{
  public abstract record Request()
  {
    public record Login(string Username, string Password) : Request()
    {
      public override string ToString()
      {
        StringBuilder builder = new();
        char[] fill = new char[Password.Length];
        Array.Fill(fill, '*');
        (this with { Password = string.Concat(fill) }).PrintMembers(builder);

        return $"Login {{ {builder} }}";
      }
    }
    public record Logout() : Request();
    public record WhoAmI() : Request();
  }

  public abstract record Response(string? Message = null)
  {
    public sealed record Ok<T>(T Data) : Response(Message: "No errors.");
    public sealed record Ok() : Response(Message: "No errors.");
    public sealed record InvalidCommand() : Response(Message: "Invalid command provided.");
    public sealed record InvalidCredentials() : Response(Message: "Invalid username or password.");
    public sealed record SessionExists() : Response(Message: "Already logged in.");
    public sealed record NoSession() : Response(Message: "Not logged in.");
    public sealed record Disconnected() : Response("Not connected.");
    public sealed record InvalidSession() : Response("Session has been invalidated.");
  }

  public Connection(ConnectionManager manager, ulong id) : base($"#{id}")
  {
    Id = id;
    Manager = manager;

    Manager.Logger.Subscribe(Logger);
  }

  public readonly ulong Id;
  public readonly ConnectionManager Manager;
  private MainResourceManager Resources => Manager.Server.Resources;
  public UserSession? Session { get; private set; }

  private async Task<Response> Handle(Request.Login loginRequest, CancellationToken cancellationToken)
  {
    if (Session != null)
    {
      return new Response.SessionExists();
    }

    var (username, password) = loginRequest;
    return await Resources.MainDatabase.RunTransaction<Response>(async (transaction, cancellationToken) =>
    {
      UserResource? user = Resources.Users.GetByUsername(transaction, username);
      if (user == null)
      {
        return new Response.InvalidCredentials();
      }

      if (Resources.UserAuthentications.GetByPassword(transaction, user.Id, password).TryGetValue(out var result))
      {
        var (authentication, hashCache) = result;
        Session = await Manager.Server.Sessions.GetUserSession(user, this, authentication, hashCache, cancellationToken);
      }
      else
      {
        return new Response.InvalidCredentials();
      }

      return new Response.Ok();
    }, cancellationToken);
  }

  private Response Handle(Request.Logout _)
  {
    Session = null;
    return new Response.Ok();
  }

  private Response.Ok<string?> Handle(Request.WhoAmI _) => new(Session?.User.Username);

  protected virtual Task<Response> OnExecute(Request request) => RunTask(async (cancellationToken) =>
  {
    if (!IsRunning)
    {
      return new Response.Disconnected();
    }
    else if (Session != null)
    {
      if (!Session.IsValid && request is not Request.Login && request is not Request.Logout)
      {
        return new Response.InvalidSession();
      }
      else if (request is Request.Login)
      {
        return new Response.SessionExists();
      }
    }
    else if (Session == null && request is not Request.Login)
    {
      return new Response.NoSession();
    }

    return request switch
    {
      Request.Login request => await Handle(request, cancellationToken),
      Request.Logout request => Handle(request),
      Request.WhoAmI request => Handle(request),

      _ => new Response.InvalidCommand()
    };
  }, CancellationToken.None);

  public async Task<Response> Execute(Request request)
  {
    Logger.Log(LogLevel.Verbose, $"> {request}");
    Response response = await OnExecute(request);
    Logger.Log(LogLevel.Verbose, $"< {response}");

    return response;
  }

  protected override Task OnRun(CancellationToken cancellationToken)
  {
    return Task.Delay(-1, cancellationToken);
  }
}
