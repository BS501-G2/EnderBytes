using System.Net;

namespace RizzziGit.EnderBytes.Extras;

public abstract record ConnectionEndPoint
{
  private ConnectionEndPoint() { }

  public sealed record Network(IPEndPoint EndPoint) : ConnectionEndPoint
  {
    public override string ToString() => EndPoint.ToString();
  }

  public sealed record Unix(string Path) : ConnectionEndPoint
  {
    public override string ToString() => Path;
  }

  public sealed record Null() : ConnectionEndPoint
  {
    public override string ToString() => "null";
  }

  public override abstract string ToString();
}
