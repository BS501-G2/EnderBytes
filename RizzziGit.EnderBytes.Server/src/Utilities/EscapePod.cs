namespace RizzziGit.EnderBytes.Utilities;

public sealed class EscapePod(byte code) : Exception()
{
  public readonly byte Code = code;
}
