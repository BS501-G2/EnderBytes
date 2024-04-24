using System.Diagnostics.CodeAnalysis;

namespace RizzziGit.EnderBytes.Web;

public sealed partial class WebApi
{
  public abstract record Result(ushort Status)
  {
    public bool TryGetValueFromResult<T>([NotNullWhen(true)] out T? value) where T : class
    {
      if (this is DataResult<T> dataResult)
      {
        value = dataResult.Data!;
        return true;
      }

      value = null;
      return false;
    }
  }
  public sealed record DataResult<T>(ushort Status, T? Data = null) : Result(Status) where T : class;
  public sealed record ErrorResult<E>(ushort Status, E? Error = null) : Result(Status) where E : class;

  public DataResult<T> Data<T>(ushort status, T? data = null) where T : class => new(status, data);
  public DataResult<object> Data(ushort status) => new(status);
  public DataResult<T> Data<T>(T data) where T : class => new(200, data);
  public DataResult<object> Data() => new(200);

  public sealed record SerializableErrorDetails(string Name, string Message, string Stack);
  public ErrorResult<E> Error<E>(ushort status, E? error = null) where E : class
  {
    return new(status, error);
  }
  public ErrorResult<object> Error(ushort status) => new(status);
  public ErrorResult<E> Error<E>(E error) where E : class => new(500, error);

  public ErrorResult<SerializableErrorDetails> Error(ushort status, Exception exception) => Error(status, new SerializableErrorDetails(exception.GetType().Name, exception.Message, exception.StackTrace ?? ""));
  public ErrorResult<SerializableErrorDetails> Error(Exception exception) => Error(500, new SerializableErrorDetails(exception.GetType().Name, exception.Message, exception.StackTrace ?? ""));

  public static bool GetValueFromResult<T>(Result result, out T? value) where T : class
  {
    if (result is DataResult<T> dataResult)
    {
      value = dataResult.Data;
      return true;
    }

    value = null;
    return false;
  }
}
