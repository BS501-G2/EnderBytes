namespace RizzziGit.EnderBytes.Connections;

using Resources;
using Utilities;
using Sessions;
using Extensions;

public abstract class Connection
{
  public abstract record Request()
  {
    public record Login(string Username, string Password) : Request();
  }

  public abstract record Response(string? Message = null)
  {
    public record Ok() : Response(Message: "No errors.");
    public record InvalidCommand() : Response(Message: "Invalid command provided.");
    public record InvalidCredentials() : Response(Message: "Invalid username or password.");
    public record SessionExists() : Response(Message: "Already logged in.");
    public record AbruptConnection(Exception? Exception) : Response();
  }

  public Connection(ConnectionManager manager, ulong id, CancellationTokenSource cancellationTokenSource)
  {
    Logger = new($"#{id}");
    Id = id;
    Manager = manager;
    CancellationTokenSource = cancellationTokenSource;
    TaskQueue = new();
    IsRunning = false;

    Manager.Logger.Subscribe(Logger);
  }

  ~Connection() => Close();

  public readonly ulong Id;
  public readonly Logger Logger;
  public readonly ConnectionManager Manager;
  private MainResourceManager Resources => Manager.Server.Resources;
  private readonly CancellationTokenSource CancellationTokenSource;
  private readonly TaskQueue TaskQueue;
  private (UserSession session, UserAuthenticationResource userAuthentication, byte[] hashCache)? Session;

  private async Task<Response> Handle(Request.Login loginRequest, CancellationToken cancellationToken)
  {
    if (Session != null)
    {
      return new Response.SessionExists();
    }

    var (username, password) = loginRequest;
    return await Resources.MainDatabase.RunTransaction<Response>(async (transaction, cancellationToken) =>
    {
      UserResource? user = Resources.Users.GetByUsername(transaction, loginRequest.Password);
      if (user == null)
      {
        return new Response.InvalidCredentials();
      }

      if (Resources.UserAuthentications.GetByPassword(transaction, user.Id, password).TryGetValue(out var result))
      {
        var (authentication, hashCache) = result;
        Session = (await Manager.Server.Sessions.GetUserSession(user, this, cancellationToken), authentication, hashCache);
      }
      else
      {
        return new Response.InvalidCredentials();
      }

      return new Response.Ok();
    }, cancellationToken);
  }

  public virtual Task<Response> Execute(Request request) => TaskQueue.RunTask(async (cancellationToken) =>
  {
    if (!IsRunning)
    {
      return new Response.AbruptConnection(Exception);
    }

    return request switch
    {
      Request.Login loginRequest => await Handle(loginRequest, cancellationToken),

      _ => new Response.InvalidCommand()
    };
  }, CancellationToken.None);


  public void Close()
  {
    try { CancellationTokenSource.Cancel(); } catch { }
  }

  private Exception? Exception;
  public bool IsRunning { get; private set; }
  public async Task Run(CancellationToken cancellationToken)
  {
    try
    {
      try
      {
        Logger.Log(LogLevel.Verbose, $"Task queue started.");
        await TaskQueue.Start(cancellationToken);
      }
      catch (Exception exception)
      {
        Exception = exception;

        throw;
      }
    }
    finally
    {
      Logger.Log(LogLevel.Verbose, $"Task queue stopped.");
      IsRunning = false;

      if (Session.TryGetValue(out var session))
      {
        session.session.Connections.Remove(this);
      }
    }
  }
}
