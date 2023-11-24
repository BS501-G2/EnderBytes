using System.Text;

namespace RizzziGit.EnderBytes.Connections;

using Resources;
using Utilities;
using Sessions;
using Extensions;

public abstract class ConnectionException : Exception
{
  private ConnectionException(string? message = null, Exception? innerException = null) : base(message, innerException)
  {

  }

  public sealed class InvalidCommand(Exception? innerException = null) : ConnectionException(innerException: innerException);
  public sealed class InvalidParameters(Exception? innerException = null) : ConnectionException(innerException: innerException);
}

public abstract class Connection : Lifetime
{
  public abstract record Request
  {
    private Request() { }
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
    public record ChangeWorkingDirectory(string[] Path, bool Relative) : Request();
    public record GetWorkingDirectory() : Request();
  }

  public abstract record Response
  {
    private Response() { }

    public sealed record Ok<T>(T Data) : Response;
    public sealed record Ok() : Response;
    public sealed record InvalidCommand() : Response;
    public sealed record InvalidCredentials() : Response;
    public sealed record SessionExists() : Response;
    public sealed record NoSession() : Response;
    public sealed record Disconnected() : Response;
    public sealed record InvalidSession() : Response;
    public sealed record InternalError() : Response;
  }

  public Connection(ConnectionManager manager, ulong id) : base($"#{id}")
  {
    Id = id;
    Manager = manager;
    Resources = manager.Server.Resources;

    Manager.Logger.Subscribe(Logger);
  }

  public readonly ulong Id;
  public readonly ConnectionManager Manager;
  private readonly ResourceManager Resources;
  public UserSession? Session { get; private set; }

  protected virtual Task<Response> OnExecute(Request request) => RunTask(async (cancellationToken) =>
  {
    return request switch
    {
      Request.Login request => await HandleLoginRequest(request, cancellationToken),
      Request.Logout => HandleLogoutRequest(),
      Request.WhoAmI => HandleWhoAmI(),
      Request.ChangeWorkingDirectory request => HandleChangeWorkingDirectoryRequest(request),

      _ => new Response.InvalidCommand()
    };
  }, CancellationToken.None);

  private Response.Ok<string> HandleWhoAmI() => new(Session?.User.Username ?? "");

  private async Task<Response> HandleLoginRequest(Request.Login command, CancellationToken cancellationToken)
  {
    if (Session != null)
    {
      return new Response.SessionExists();
    }

    try
    {
      var (user, authentication, hashCache) = await Resources.Database.RunTransaction((transaction) =>
      {
        UserResource user = Resources.Users.GetByUsername(transaction, command.Username) ?? throw new EscapePod(0);

        if (Resources.UserAuthentications.GetByPassword(transaction, user, command.Password).TryGetValue(out var result))
        {
          var (authentication, hashCache) = result;
          return (user, authentication, hashCache);
        }

        throw new EscapePod(0);
      }, cancellationToken);

      Session = await Manager.Server.Sessions.GetUserSession(user, this, authentication, hashCache, cancellationToken);
      return new Response.Ok();
    }
    catch (EscapePod pod)
    {
      return pod.Code switch
      {
        0 => new Response.InvalidCredentials(),
        _ => new Response.InternalError(),
      };
    }
  }

  private Response HandleLogoutRequest()
  {
    Session = null;
    return new Response.Ok();
  }

  private static Response HandleChangeWorkingDirectoryRequest(Request.ChangeWorkingDirectory request)
  {
    return new Response.Ok();
  }

  private async Task<Response> WrapExecute(Request request)
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

    return await OnExecute(request);
  }

  public async Task<Response> Execute(Request request)
  {
    Logger.Log(LogLevel.Verbose, $"> {request}");
    Response response = await WrapExecute(request);
    Logger.Log(LogLevel.Verbose, $"< {response}");

    return response;
  }

  protected override Task OnRun(CancellationToken cancellationToken)
  {

    return Task.Delay(-1, cancellationToken);
  }
}
