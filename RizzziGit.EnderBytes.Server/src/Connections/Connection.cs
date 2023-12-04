using System.Text;

namespace RizzziGit.EnderBytes.Connections;

using Resources;
using Utilities;
using Sessions;
using Extensions;
using StoragePools;

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

    public sealed record NotFound() : Response;
  }

  public Connection(ConnectionManager manager, ulong id) : base($"#{id}")
  {
    Id = id;
    Manager = manager;
    Resources = manager.Server.Resources;

    Manager.Logger.Subscribe(Logger);
  }

  private readonly ResourceManager Resources;

  private StoragePool.FolderNode? CurrentNode;

  public readonly ulong Id;
  public readonly ConnectionManager Manager;
  public UserSession? Session { get; private set; }

  protected virtual Task<Response> OnExecute(Request request) => RunTask<Response>(async (cancellationToken) =>
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

    switch (request)
    {
      case Request.Login loginRequest:
        if (Session != null)
        {
          return new Response.SessionExists();
        }

        try
        {
          var (user, authentication, hashCache) = await Resources.Database.RunTransaction((transaction) =>
          {
            UserResource user = Resources.Users.GetByUsername(transaction, loginRequest.Username) ?? throw new EscapePod(0);

            if (Resources.UserAuthentications.GetByPassword(transaction, user, loginRequest.Password).TryGetValue(out var result))
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

      case Request.Logout:
        Session = null;
        return new Response.Ok();

      case Request.ChangeWorkingDirectory request:
        await Resources.Database.RunTransaction((transaction) =>
        {
          foreach (MountPointResource mountPoint in Resources.MountPoints.Stream(transaction, Session!.User))
          {

          }
        }, cancellationToken);

        // return new Response.Ok();
        break;
    }

    return new Response.InvalidCommand();
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
