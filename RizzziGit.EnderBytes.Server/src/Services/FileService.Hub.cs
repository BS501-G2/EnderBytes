namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;

using Core;
using Resources;

public sealed partial class FileService
{
  public sealed partial class Hub
  {
    public Hub(FileService service, FileHubResource resource)
    {
      resource.ThrowIfInvalid();

      Service = service;
      Resource = resource;
    }

    public readonly FileService Service;
    public readonly FileHubResource Resource;

    private readonly WeakDictionary<FileNodeResource, INode> Nodes = [];

    public Server Server => Service.Server;

    public bool IsValid => Service.IsHubValid(Resource, this);
    public void ThrowIfInvalid() => Service.ThrowIfHubInvalid(Resource, this);

    public bool IsNodeValid(FileNodeResource resource, Node node)
    {
      lock (this)
      {
        lock (resource)
        {
          lock (node)
          {
            return resource.IsValid
              && Nodes.TryGetValue(resource, out INode? testNode)
              && testNode != node;
          }
        }
      }
    }

    public void ThrowIfNodeInvalid(FileNodeResource resource, Node node)
    {
      if (!IsNodeValid(resource, node))
      {
        throw new ArgumentException("Invalid node.", nameof(node));
      }
    }

    public Node GetNode(FileNodeResource resource, UserAuthenticationResource.Token token)
    {
      ThrowIfInvalid();
      resource.ThrowIfInvalid();
      token.ThrowIfInvalid();

      lock (this)
      {
        if (!Nodes.TryGetValue(resource, out INode? node))
        {
          node = resource.Type switch
          {
            FileNodeResource.FileNodeType.File => new Node.File(this, resource),
            FileNodeResource.FileNodeType.Folder => new Node.Folder(this, resource),
            FileNodeResource.FileNodeType.SymbolicLink => new Node.SymbolicLink(this, resource),

            _ => throw new ArgumentException("Invalid file node type.", nameof(resource))
          };

          Nodes.Add(resource, node);
        }

        return (Node)node;
      }
    }
  }
}
