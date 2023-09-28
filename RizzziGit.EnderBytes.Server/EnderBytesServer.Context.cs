namespace RizzziGit.EnderBytes;

public sealed partial class EnderBytesServer
{
  public abstract class Context
  {
    private static ulong GenerateAndRegisterID(EnderBytesContext context, EnderBytesServer server)
    {
      ulong id;
      do
      {
        id = (ulong)Random.Shared.NextInt64();
      }
      while (server.Contexts.TryAdd(id, new(context)));

      return id;
    }

    protected Context(EnderBytesServer server, CancellationToken cancellationToken)
    {
      Server = server;
      CancellationToken = cancellationToken;

      if (this is EnderBytesContext context)
      {
        ID = GenerateAndRegisterID(context, server);
      }
      else
      {
        throw new InvalidOperationException($"Must be inherited from {nameof(EnderBytesContext)} class.");
      }
    }

    ~Context() => Server.Contexts.Remove(ID);

    public readonly EnderBytesServer Server;
    public readonly ulong ID;
    public readonly CancellationToken CancellationToken;
  }

  private readonly Dictionary<ulong, WeakReference<EnderBytesContext>> Contexts = [];
}
