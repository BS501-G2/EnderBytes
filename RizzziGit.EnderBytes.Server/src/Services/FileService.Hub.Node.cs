namespace RizzziGit.EnderBytes.Services;

using Resources;

public sealed partial class FileService
{
  public sealed partial class Hub
  {
    private interface INode;

    public abstract partial class Node
    {
      private Node(Hub hub, FileNodeResource resource, FileNodeResource.FileNodeType type)
      {
        resource.ThrowIfInvalid();
        if (resource.Type != type)
        {
          throw new ArgumentException("Invalid node type.", nameof(resource));
        }

        Hub = hub;
        Resource = resource;
      }

      public readonly Hub Hub;
      public readonly FileNodeResource Resource;

      public bool IsValid => Hub.IsNodeValid(Resource, this);
      public void ThrowIfInvalid() => Hub.ThrowIfNodeInvalid(Resource, this);
    }
  }
}
