namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;

using Resources;

public sealed partial class FileService
{
  public sealed partial class Hub
  {
    public sealed class Context(Hub hub, ConnectionService.Connection connection, SessionService.Session? session)
    {
      public sealed class FileNodeHandle()
      {

      }

      public readonly Hub Hub = hub;
      public readonly ConnectionService.Connection Connection = connection;
      public readonly SessionService.Session? Session = session;

      public bool IsValid => Hub.IsContextValid(Connection, this);
      public void ThrowIfInvalid() => Hub.ThrowIfContextInvalid(Connection, this);
    }

    private readonly WeakDictionary<Node.File, LinkedList<Node.File.Cache>> FileCache = [];
  }
}
