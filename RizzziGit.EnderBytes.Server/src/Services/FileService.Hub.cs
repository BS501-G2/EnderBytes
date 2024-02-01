namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;

using Resources;

public sealed partial class FileService
{
  public sealed partial class Hub
  {
    public Hub(FileService service, FileHubResource resource)
    {
      resource.ThrowIfInvalid();

      Contexts = [];

      Service = service;
      Resource = resource;
    }

    public readonly FileService Service;
    public readonly FileHubResource Resource;

    private readonly WeakDictionary<ConnectionService.Connection, Context> Contexts;

    public bool IsValid => Service.IsHubValid(Resource, this);
    public void ThrowIfInvalid() => Service.ThrowIfHubInvalid(Resource, this);

    public Context GetContext(ConnectionService.Connection connection)
    {
      lock (this)
      {
        if (!Contexts.TryGetValue(connection, out Context? context) || connection != context.Connection || connection.Session != context.Session)
        {
          Contexts.AddOrUpdate(connection, context = new(this, connection, connection.Session));
        }

        return context;
      }
    }

    public bool IsContextValid(ConnectionService.Connection connection, Context context)
    {
      lock (this)
      {
        return IsValid &&
          connection.Session?.IsValid != false &&
          Contexts.TryGetValue(connection, out Context? testBinding) &&
          testBinding == context &&
          connection.Session == testBinding.Session;
      }
    }

    public void ThrowIfContextInvalid(ConnectionService.Connection connection, Context context)
    {
      if (!IsContextValid(connection, context))
      {
        throw new ArgumentException("Invalid context.", nameof(context));
      }
    }
  }
}
