using System.Net;

namespace RizzziGit.EnderBytes.Services;

public sealed partial class ConnectionService
{
  public abstract record EndPoint
  {
    private EndPoint() { }

    public sealed record Network(IPEndPoint EndPoint) : EndPoint
    {
      public override string ToString() => EndPoint.ToString();
    }

    public sealed record Unix(string Path) : EndPoint
    {
      public override string ToString() => Path;
    }

    public sealed record Null() : EndPoint
    {
      public override string ToString() => "null";
    }

    public override abstract string ToString();
  }
}
