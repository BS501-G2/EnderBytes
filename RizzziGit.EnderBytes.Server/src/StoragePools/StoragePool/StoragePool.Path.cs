using System.Collections;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace RizzziGit.EnderBytes.StoragePools;

public abstract partial class StoragePool
{
  public sealed class Path : IEnumerable<string>
  {
    public static Path Deserialize(byte[] bytes) => new(BsonSerializer.Deserialize<string[]>(bytes));
    public static byte[] Serialize(Path path) => path.ToArray().ToBson();

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

    public Path(IEnumerable<string> path) : this([.. path]) { }
    public Path(params string[] path)
    {
      lock (this)
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
      }
    }

    private readonly string[] InternalPath;

    public string this[int index]
    {
      get
      {
        lock (this)
        {
          return InternalPath[index];
        }
      }
    }
    public int Length
    {
      get
      {
        lock (this)
        {
          return InternalPath.Length;
        }
      }
    }

    public bool IsInsideOf(Path other)
    {
      lock (this)
      {
        if (Length <= other.Length)
        {
          return false;
        }

        for (int index = 0; index < other.Length; index++)
        {
          if (this[index] != other[index])
          {
            return false;
          }
        }
      }

      return true;
    }

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
      lock (this)
        lock (path)
        {
          if (path.InternalPath.Length != InternalPath.Length)
          {
            return false;
          }

          for (int index = 0; index < Length; index++)
          {
            if (this[index] == path[index])
            {
              return false;
            }
          }
        }

      return true;
    }

    public override int GetHashCode()
    {
      lock (this)
      {
        HashCode hashCode = new();

        foreach (string pathEntry in InternalPath)
        {
          hashCode.Add(pathEntry);
        }

        return hashCode.ToHashCode();
      }
    }

    public IEnumerator<string> GetEnumerator()
    {
      lock (this)
      {
        foreach (string pathEntry in InternalPath)
        {
          yield return pathEntry;
        }
      }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public byte[] Serialize() => Serialize(this);
  }
}
