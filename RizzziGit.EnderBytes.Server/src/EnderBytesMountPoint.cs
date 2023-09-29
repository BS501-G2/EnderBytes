namespace RizzziGit.EnderBytes;

public abstract class EnderBytesMountPoint()
{

}

public sealed partial class EnderBytesServer
{
  private readonly Dictionary<ulong, EnderBytesMountPoint> MountPoints = [];
}
