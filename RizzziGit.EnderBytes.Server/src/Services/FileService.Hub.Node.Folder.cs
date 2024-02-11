using System.Runtime.CompilerServices;

namespace RizzziGit.EnderBytes.Services;

using Resources;

public sealed partial class FileService
{
  public sealed partial class Hub
  {
    public abstract partial class Node
    {
      public sealed class Folder(Hub hub, FileResource resource) : Node(hub, resource, FileResource.FileNodeType.Folder)
      {
        public async IAsyncEnumerable<Node> Scan(UserAuthenticationResource.Token token, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
          token.ThrowIfInvalid();
          await foreach (FileResource file in ResourceService.EnumeratedTransact((transaction, service, cancellationToken) => ResourceService.Files.Scan(transaction, Hub.Resource, Resource, token, cancellationToken: cancellationToken), cancellationToken))
          {
            yield return Hub.GetNode(Resource);
          }
        }

        public async Task<Node> CreateFolder(UserAuthenticationResource.Token token, string name, CancellationToken cancellationToken = default) => Hub.GetNode(await ResourceService.Transact((transaction, service, cancellationToken) => service.Files.Create(transaction, Hub.Resource, Resource, FileResource.FileNodeType.Folder, name, token), cancellationToken));
      }
    }
  }
}
