namespace RizzziGit.EnderBytes;

public static class Program
{
  public static async Task Main(string[] args)
  {
    EnderBytesServer server = new();

    await server.Init(CancellationToken.None);
  }
}
