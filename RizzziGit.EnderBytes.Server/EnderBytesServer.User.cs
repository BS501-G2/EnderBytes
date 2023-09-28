namespace RizzziGit.EnderBytes;

public sealed partial class EnderBytesServer
{
  public abstract class User
  {
    public User()
    {
    }
  }

  private readonly Dictionary<ulong, WeakReference<EnderBytesUser>> Users = [];
}
