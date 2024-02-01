namespace RizzziGit.EnderBytes.Services;

using Framework.Memory;

using Resources;

public sealed partial class FileService
{
  public sealed partial class Hub
  {
    public abstract class Node
    {
      private Node(Hub hub, FileNodeResource resource)
      {
        Hub = hub;
        Resource = resource;
      }

      public readonly Hub Hub;
      public readonly FileNodeResource Resource;

      public sealed class File(Hub hub, FileNodeResource resource) : Node(hub, resource)
      {
        public sealed record Cache(long Begin, CompositeBuffer Buffer)
        {
          public long End => Begin + Buffer.Length;
        }
      }

      public sealed class Folder(Hub hub, FileNodeResource resource) : Node(hub, resource)
      {
      }

      public sealed class SymbolicLink(Hub hub, FileNodeResource resource) : Node(hub, resource)
      {
      }
    }
  }
}
