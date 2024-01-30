namespace RizzziGit.EnderBytes.Resources;

using Core;

public sealed partial class FileService(Server server) : Server.SubService(server, "Blobs")
{
}
