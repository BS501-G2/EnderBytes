using RizzziGit.EnderBytes.Resources;

namespace RizzziGit.EnderBytes.Services;

public sealed partial class FileService
{
  public sealed partial class Hub
  {
    public sealed class TrashManager(Hub hub)
    {
      // public sealed record TrashEntry(Node Node, long TrashTime, );

      // public readonly Hub Hub = hub;

      // public FileService Service => Hub.Service;
      // public ResourceService ResourceService => Hub.ResourceService;

      // public async IAsyncEnumerable<Node> ListTrash(CancellationToken cancellationToken = default)
      // {
      //   ResourceService.EnumeratedTransact<FileResource>((transaction, service, cancellationToken) =>
      //   {
      //   }, cancellationToken);
      // }
    }
  }
}
