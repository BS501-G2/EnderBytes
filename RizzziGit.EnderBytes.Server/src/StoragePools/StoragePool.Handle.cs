namespace RizzziGit.EnderBytes.StoragePools;

using Connections;

public abstract partial class StoragePool
{
  public class Context(StoragePool pool, Connection connection)
  {
    public readonly StoragePool Pool = pool;
    public readonly Connection Connection = connection;
  }

  public abstract partial class Node
  {
    public static bool IsValidHandle(Node handle) => handle is not FileNode && handle is not FolderNode && handle is not SymbolicLinkNode;
    public static void ValidateHandle(Node handle)
    {
      if (IsValidHandle(handle))
      {
        return;
      }

      throw new Exception.InvalidHandleType();
    }

    protected Node(StoragePool pool)
    {
      Pool = pool;
    }

    public readonly StoragePool Pool;

    public abstract long Id { get; }
    public abstract FolderNode? Parent { get; }
    public abstract string Name { get; }

    public Path Path => new(Parent != null ? [.. Parent.Path, Name] : [Name]);

    public Task SetParent(Context context, FolderNode? parent) => Pool.Internal_SetParent(context, this, parent);
    public Task SetName(Context context, string newName) => Pool.Internal_SetName(context, this, newName);
  }

  public abstract class FileNode : Node
  {
    protected FileNode(StoragePool pool) : base(pool)
    {
    }

    public abstract long Size { get; }
  }

  public abstract class FolderNode : Node
  {
    protected FolderNode(StoragePool pool) : base(pool)
    {
    }

    public Task<Node[]> Scan(Context context) => Pool.Internal_Scan(context, this);

    public Task<FolderNode> CreateFolder(Context context, string name) => Pool.Internal_CreateFolder(context, this, name);
    public Task<FileNode> CreateFile(Context context, string name, long preallocateSize = 0) => Pool.Internal_CreateFile(context, this, name, preallocateSize);
    public Task<SymbolicLinkNode> CreateSymbolicLink(Context context, string name, Path target) => Pool.Internal_CreateSymbolicLink(context, this, name, target);
  }

  public abstract class SymbolicLinkNode : Node
  {
    protected SymbolicLinkNode(StoragePool pool) : base(pool)
    {
    }

    public abstract Path TargetPath { get; }
  }

  protected abstract Task Internal_SetParent(Context context, Node node, FolderNode? parent);
  protected abstract Task Internal_SetName(Context context, Node node, string name);

  protected abstract Task<Node[]> Internal_Scan(Context context, FolderNode folder);

  protected abstract Task<FolderNode> Internal_CreateFolder(Context context, FolderNode parent, string name);
  protected abstract Task<FileNode> Internal_CreateFile(Context context, FolderNode parent, string name, long preallocateSize);
  protected abstract Task<SymbolicLinkNode> Internal_CreateSymbolicLink(Context context, FolderNode parent, string name, Path target);
}
