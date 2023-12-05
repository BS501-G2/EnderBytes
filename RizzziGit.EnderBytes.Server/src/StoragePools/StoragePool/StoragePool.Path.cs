using System.Collections;

namespace  RizzziGit.EnderBytes.StoragePools;

public abstract partial class StoragePool
{
  public sealed class Path : IEnumerable<string>
  {
    public static bool operator !=(Path? path1, Path? path2) => !(path1 == path2);
    public static bool operator ==(Path? path1, Path? path2)
    {
      if ((path1 is null && path2 is null) || ReferenceEquals(path1, path2))
      {
        return true;
      }
      else if (path1 is null || path2 is null)
      {
        return false;
      }

      return path1.Equals(path2);
    }

    public Path(params string[] path) : this(false, path) { }
    public Path(bool ignoreCase = false, params string[] path)
    {
      {
        List<string> sanitized = [];
        foreach (string pathEntry in path)
        {
          if (
            (pathEntry.Length == 0) ||
            (pathEntry == ".")
          )
          {
            continue;
          }
          else if (pathEntry == "..")
          {
            if (sanitized.Count == 0)
            {
              throw new ArgumentException("Invalid path.", nameof(path));
            }

            sanitized.RemoveAt(sanitized.Count - 1);
            continue;
          }

          sanitized.Add(pathEntry);
        }

        InternalPath = [.. sanitized];
        sanitized.Clear();
      }
    }

    private readonly string[] InternalPath;
    public bool IgnoreCase = false;

    public string this[int index] => InternalPath[index];
    public int Length => InternalPath.Length;

    public bool IsInsideOf(Path other)
    {
      if (Length <= other.Length)
      {
        return false;
      }

      for (int index = 0; index < other.Length; index++)
      {
        if (string.Equals(
          this[index],
          other[index],
            IgnoreCase
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal
        ))
        {
          return false;
        }
      }

      return true;
    }

    public bool EqualsAt(int index, string test) => string.Equals(
      this[index],
      test,
      IgnoreCase
        ? StringComparison.InvariantCultureIgnoreCase
        : StringComparison.InvariantCulture
    );

    public override bool Equals(object? obj)
    {
      if (ReferenceEquals(this, obj))
      {
        return true;
      }
      else if (obj is null || obj is not Path)
      {
        return false;
      }

      Path path = (Path)obj;
      if (path.InternalPath.Length != InternalPath.Length)
      {
        return false;
      }

      for (int index = 0; index < Length; index++)
      {
        if (EqualsAt(index, path[index]))
        {
          return false;
        }
      }

      return true;
    }

    public override int GetHashCode()
    {
      HashCode hashCode = new();

      foreach (string pathEntry in InternalPath)
      {
        hashCode.Add(pathEntry);
      }

      return hashCode.ToHashCode();
    }

    public IEnumerator<string> GetEnumerator()
    {
      foreach (string pathEntry in InternalPath)
      {
        yield return pathEntry;
      }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  }

}
