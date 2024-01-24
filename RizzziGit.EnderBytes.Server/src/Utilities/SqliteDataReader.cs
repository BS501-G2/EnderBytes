using System.Data.SQLite;

namespace RizzziGit.EnderBytes.Utilities;

public static class SQLiteDataReaderExtensions
{
  public static string? GetStringOptional(this SQLiteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
  public static char? GetCharOptional(this SQLiteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetChar(ordinal);
  public static DateTime? GetDateTimeOptional(this SQLiteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
  // public static DateTimeOffset? GetDateTimeOffsetOptional(this SQLiteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDateTimeOffset(ordinal);
  public static decimal? GetDecimalOptional(this SQLiteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
  public static double? GetDoubleOptional(this SQLiteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDouble(ordinal);
  public static float? GetFloatOptional(this SQLiteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetFloat(ordinal);
  public static Guid? GetGuidOptional(this SQLiteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
  public static Stream? GetStreamOptional(this SQLiteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetStream(ordinal);
  public static TextReader? GetTextReaderOptional(this SQLiteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetTextReader(ordinal);
  // public static TimeSpan? GetTimeSpanOptional(this SQLiteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetTimeSpan(ordinal);
  public static bool? GetBooleanOptional(this SQLiteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetBoolean(ordinal);
  public static long? GetInt64Optional(this SQLiteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
  public static int? GetInt32Optional(this SQLiteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
  public static short? GetInt16Optional(this SQLiteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetInt16(ordinal);
  public static byte? GetByteOptional(this SQLiteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetByte(ordinal);
  public static object? GetValueOptional(this SQLiteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);

  public static long GetBytesOptional(this SQLiteDataReader reader, int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
  {
    if (reader.IsDBNull(ordinal))
    {
      return 0;
    }

    return reader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
  }

  public static long GetCharsOptional(this SQLiteDataReader reader, int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
  {
    if (reader.IsDBNull(ordinal))
    {
      return 0;
    }

    return reader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
  }
}
