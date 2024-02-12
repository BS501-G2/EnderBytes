namespace RizzziGit.EnderBytes.Services;

using Resources;

public sealed partial class FileService
{
  public sealed partial class Hub
  {
    public abstract partial class Node
    {
      private Node(Hub hub, FileResource resource, FileResource.FileNodeType type)
      {
        if (resource.Type != type)
        {
          throw new ArgumentException("Invalid node type.", nameof(resource));
        }

        Hub = hub;
        Resource = resource;
      }

      public readonly Hub Hub;
      public readonly FileResource Resource;

      private ResourceService ResourceService => Hub.ResourceService;

      public bool IsValid => Hub.IsNodeValid(Resource, this);
      public void ThrowIfInvalid() => Hub.ThrowIfNodeInvalid(Resource, this);

      public async Task Trash(UserAuthenticationResource.Token token)
      {
        ThrowIfInvalid();
      }
    }
  }
}
