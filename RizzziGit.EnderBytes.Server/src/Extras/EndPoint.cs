using System.Net;

namespace RizzziGit.EnderBytes.Extras;

public abstract record ClientEndPoint
{
    private ClientEndPoint() { }

    public sealed record Network(IPEndPoint EndPoint) : ClientEndPoint
    {
        public override string ToString() => EndPoint.ToString();
    }

    public sealed record Unix(string Path) : ClientEndPoint
    {
        public override string ToString() => Path;
    }

    public sealed record Null() : ClientEndPoint
    {
        public override string ToString() => "null";
    }

    public override abstract string ToString();
}
