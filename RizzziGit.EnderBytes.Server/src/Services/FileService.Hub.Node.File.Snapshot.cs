namespace RizzziGit.EnderBytes.Services;

public sealed partial class FileService
{
  public sealed partial class Hub
  {
    public abstract partial class Node
    {
      public sealed partial class File
      {
        public sealed class Snapshot(File file)
        {
          public readonly File File = file;
        }
      }
    }
  }
}
