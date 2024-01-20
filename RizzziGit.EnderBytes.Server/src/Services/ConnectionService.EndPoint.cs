using System.Net;

namespace RizzziGit.EnderBytes.Services;

public sealed partial class ConnectionService
{
  public abstract record EndPoint
  {
    private EndPoint() { }

    public sealed record Network(IPAddress Address, long Port) : EndPoint;
    public sealed record Unix(string Path) : EndPoint;
    public sealed record Null() : EndPoint;
  }
}
