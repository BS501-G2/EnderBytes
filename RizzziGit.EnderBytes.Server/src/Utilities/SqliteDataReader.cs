using System.Data.Common;
using System.Text;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderBytes.Utilities;

public static class DbExtensions
{
    public static string? GetStringOptional(this DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    public static char? GetCharOptional(this DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetChar(ordinal);
    public static DateTime? GetDateTimeOptional(this DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    // public static DateTimeOffset? GetDateTimeOffsetOptional(this DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDateTimeOffset(ordinal);
    public static decimal? GetDecimalOptional(this DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    public static double? GetDoubleOptional(this DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDouble(ordinal);
    public static float? GetFloatOptional(this DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetFloat(ordinal);
    public static Guid? GetGuidOptional(this DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
    public static Stream? GetStreamOptional(this DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetStream(ordinal);
    public static TextReader? GetTextReaderOptional(this DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetTextReader(ordinal);
    // public static TimeSpan? GetTimeSpanOptional(this DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetTimeSpan(ordinal);
    public static bool? GetBooleanOptional(this DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetBoolean(ordinal);
    public static long? GetInt64Optional(this DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    public static int? GetInt32Optional(this DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    public static short? GetInt16Optional(this DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetInt16(ordinal);
    public static byte? GetByteOptional(this DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetByte(ordinal);
    public static object? GetValueOptional(this DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);

    public static JObject GetJsonObject(this DbDataReader reader, int ordinal) => new(Encoding.UTF8.GetString(reader.GetBytes(ordinal)));
    public static JObject? GetJsonObjectOptional(this DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetJsonObject(ordinal);

    public static byte[] GetBytes(this DbDataReader reader, int ordinal) => (byte[])reader.GetValue(ordinal);
    public static byte[]? GetBytesOptional(this DbDataReader reader, int ordinal) => (byte[]?)reader.GetValueOptional(ordinal);

    public static long GetBytesOptional(this DbDataReader reader, int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
    {
        if (reader.IsDBNull(ordinal))
        {
            return 0;
        }

        return reader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
    }

    public static long GetCharsOptional(this DbDataReader reader, int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
    {
        if (reader.IsDBNull(ordinal))
        {
            return 0;
        }

        return reader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
    }
}
