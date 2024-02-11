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
      Service = service;
      Resource = resource;
    }

    public readonly FileService Service;
    public readonly FileHubResource Resource;

    private readonly WeakDictionary<FileResource, Node> Nodes = [];

    private Server Server => Service.Server;
    private ResourceService ResourceService => Server.ResourceService;

    public async Task<Node.Folder> GetRootFolder(UserAuthenticationResource.Token token) => (Node.Folder)GetNode(await ResourceService.Transact((transaction, service, cancellationToken) => service.Files.GetRootFolder(transaction, Resource, token)));
    public async Task<Node.Folder> GetTrashFolder(UserAuthenticationResource.Token token) => (Node.Folder)GetNode(await ResourceService.Transact((transaction, service, cancellationToken) => service.Files.GetTrashFolder(transaction, Resource, token)));
    public async Task<Node.Folder> GetInternalFolder(UserAuthenticationResource.Token token) => (Node.Folder)GetNode(await ResourceService.Transact((transaction, service, cancellationToken) => service.Files.GetInternalFolder(transaction, Resource, token)));

    public bool IsNodeValid(FileResource resource, Node node)
    {
      lock (this)
      {
        lock (resource)
        {
          lock (node)
          {
            return resource.IsValid
              && Nodes.TryGetValue(resource, out Node? testNode)
              && testNode != node;
          }
        }
      }
    }

    public void ThrowIfNodeInvalid(FileResource resource, Node node)
    {
      if (!IsNodeValid(resource, node))
      {
        throw new ArgumentException("Invalid node.", nameof(node));
      }
    }

    public Node GetNode(FileResource resource)
    {
      lock (this)
      {
        lock (resource)
        {
          if (!Nodes.TryGetValue(resource, out Node? node) || !node.IsValid)
          {
            node = resource.Type switch
            {
              FileResource.FileNodeType.File => new Node.File(this, resource),
              FileResource.FileNodeType.Folder => new Node.Folder(this, resource),
              FileResource.FileNodeType.SymbolicLink => new Node.SymbolicLink(this, resource),

              _ => throw new ArgumentException("Invalid file node type.", nameof(resource))
            };

            Nodes.AddOrUpdate(resource, node);
          }

          return node;
        }
      }
    }
  }
}
