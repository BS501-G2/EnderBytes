namespace RizzziGit.EnderBytes;

public struct EnderBytesConfig()
{
  public int DefaultPasswordIterations = 10000;
  public int DefaultBlobStorageFileBufferSize = 1024 * 256;
  public long ObsolescenceTimeSpan = 1000L * 60 * 60 * 24 * 30;

  public string DatabaseDir = Path.Join(Environment.CurrentDirectory, ".db");
}
