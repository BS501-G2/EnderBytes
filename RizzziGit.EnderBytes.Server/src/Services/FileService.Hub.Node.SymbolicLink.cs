namespace RizzziGit.EnderBytes.Services;

using Resources;

public sealed partial class FileService
{
  public sealed partial class Hub
  {
    public abstract partial class Node
    {
      public sealed class SymbolicLink(Hub hub, FileResource resource) : Node(hub, resource, FileResource.FileNodeType.SymbolicLink)
      {
      }
    }
  }
}
