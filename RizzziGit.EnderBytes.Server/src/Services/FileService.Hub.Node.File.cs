namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;

using Resources;

public sealed partial class FileService
{
  [Flags]
  public enum FileAccess
  {
    Read = 0b001,
    Write = 0b010,
    Exclusive = 0b100
  }

  public sealed partial class Hub
  {
    public abstract partial class Node
    {
      public class File(Hub hub, FileNodeResource resource) : Node(hub, resource, FileNodeResource.FileNodeType.File), INode
      {
        public class Handle()
        {

        }

        private readonly WeakDictionary<long, Handle> Handles = [];
      }
    }
  }
}
