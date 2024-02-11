namespace RizzziGit.EnderBytes.Services;

using Framework.Collections;

using Resources;

public sealed partial class FileService
{
  [Flags]
  public enum FileAccess
  {
    Read = 0b001,
    Write = 0b010,
    Exclusive = 0b100
  }

  public sealed partial class Hub
  {
    public abstract partial class Node
    {
      public sealed partial class File(Hub hub, FileResource resource) : Node(hub, resource, FileResource.FileNodeType.File)
      {
        private readonly WeakDictionary<File, List<Handle>> Handles = [];

        public bool IsHandleValid(File file, Handle handle)
        {
          lock (this)
          {
            lock (file)
            {
              lock (handle)
              {
                return Handles.TryGetValue(file, out List<Handle>? testValue)
                  && testValue.Contains(handle);
              }
            }
          }
        }

        public void ThrowIfHandleInvalid(File file, Handle handle)
        {
          if (!IsHandleValid(file, handle))
          {
            throw new ArgumentException("Invalid file handle.", nameof(handle));
          }
        }
      }
    }
  }
}
