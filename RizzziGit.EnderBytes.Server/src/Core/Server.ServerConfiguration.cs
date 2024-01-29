using System.Net;

namespace RizzziGit.EnderBytes.Core;

public sealed partial class Server
{
  public sealed record ServerConfiguration(
    string? WorkingPath = null,

    int KeyGeneratorThreads = 4,
    int MaxPregeneratedKeyCount = 1000,

    IPEndPoint? FtpAddress = null
  )
  {
    public string WorkingPath = WorkingPath ?? Path.Join(Environment.CurrentDirectory, ".EnderBytes");
  }
}
