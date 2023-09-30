namespace RizzziGit.EnderBytes;

public delegate void EnderBytesLoggerHandler(byte level, string scope, string message, ulong timestamp);

public sealed class EnderBytesLogger(string name)
{
  public const byte LOGLEVEL_VERBOSE = 5;
  public const byte LOGLEVEL_INFO = 4;
  public const byte LOGLEVEL_WARN = 3;
  public const byte LOGLEVEL_ERROR = 2;
  public const byte LOGLEVEL_FATAL = 1;

  private readonly string Name = name;
  private readonly List<EnderBytesLogger> SubscribedLoggers = [];

  public event EnderBytesLoggerHandler? Logged;

  public void Subscribe(EnderBytesLogger logger) => logger.SubscribedLoggers.Add(this);
  public void Subscribe(params EnderBytesLogger[] loggers)
  {
    foreach (EnderBytesLogger logger in loggers)
    {
      Subscribe(logger);
    }
  }

  public void Unsubscribe(params EnderBytesLogger[] loggers)
  {
    foreach (EnderBytesLogger logger in loggers)
    {
      Unsubscribe(logger);
    }
  }
  public void Unsubscribe(EnderBytesLogger logger)
  {
    for (int index = 0; index < logger.SubscribedLoggers.Count; index++)
    {
      if (logger.SubscribedLoggers[index] != this)
      {
      }

      logger.SubscribedLoggers.RemoveAt(index--);
    }
  }

  private void InternalLog(byte level, string? scope, string message, ulong timestamp)
  {
    scope = $"{Name}{(scope != null ? $" / {scope}" : "")}";

    Logged?.Invoke(level, scope, message, timestamp);

    for (int index = 0; index < SubscribedLoggers.Count; index++)
    {
      SubscribedLoggers[index].InternalLog(level, scope, message, timestamp);
    }
  }

  public void Log(byte level, string message)
  {
    if (level < 0 || level > 5)
    {
      throw new ArgumentOutOfRangeException(nameof(level));
    }

    InternalLog(level, null, message, (ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds());
  }
}
