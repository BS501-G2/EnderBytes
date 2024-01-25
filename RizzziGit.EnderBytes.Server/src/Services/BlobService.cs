namespace RizzziGit.EnderBytes.Resources;

using Core;

public sealed partial class BlobService(Server server) : Server.SubService(server, "Blobs")
{
}
