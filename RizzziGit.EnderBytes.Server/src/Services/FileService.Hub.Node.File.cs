namespace RizzziGit.EnderBytes.Services;

using Resources;

public sealed partial class FileService
{
  public sealed partial class Hub
  {
    public abstract partial class Node
    {
      public class File(Hub hub, FileNodeResource resource) : Node(hub, resource, FileNodeResource.FileNodeType.File), INode
      {
      }
    }
  }
}
