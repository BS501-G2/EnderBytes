namespace RizzziGit.EnderBytes.StoragePools;

using Connections;
using Resources.BlobStorage;

public sealed partial class BlobStoragePool
{
  private new Root? Root;

  private sealed class BlobContext(BlobStoragePool pool, Connection connection) : Context(pool, connection)
  {
  }
}
