namespace RizzziGit.EnderBytes.Connections;

public record ConnectionResponse(byte Code)
{
  public const byte CODE_OK = 0;
  public const byte CODE_INVALID_COMMAND = 1;
  public const byte CODE_UNSUPORTED_COMMAND = 2;
  public const byte CODE_INVALID_OPERATION = 1;
}
