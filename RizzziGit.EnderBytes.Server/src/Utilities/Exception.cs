namespace RizzziGit.EnderBytes.Utilities;

public static class ExceptionExtensions
{
  public static string Stringify<T>(this T exception) where T : Exception => $"[{typeof(T).Name}] {exception.Message}{(exception.StackTrace != null ? $"\n{exception.StackTrace}" : "")}";
  public static bool IsDueToCancellationToken<T>(this T exception, CancellationToken cancellationToken) where T : Exception => exception is OperationCanceledException operationCanceledException && operationCanceledException.CancellationToken == cancellationToken;
}
