namespace RizzziGit.EnderBytes.Protocols;

public sealed partial class FtpProtocol
{
  private sealed record Reply(int Code, string Message);
}
