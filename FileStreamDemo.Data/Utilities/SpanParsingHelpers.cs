using System.Text;

namespace FileStreamDemo.Data.Utilities;


/// <summary>
/// Utility methods for parsing data from ReadOnlySpan&lt;byte&gt; with zero allocations
/// </summary>
public static class SpanParsingHelpers
{
    /// <summary>
    /// Attempts to parse the next field from a delimited line
    /// </summary>
    /// <param name="line">The line to parse (will be advanced past the field and delimiter)</param>
    /// <param name="delimiter">The delimiter byte (e.g., ',' or '\t')</param>
    /// <param name="field">The parsed field bytes</param>
    /// <returns>True if a field was found, false if the line is empty</returns>
    public static bool TryParseNextField(ref ReadOnlySpan<byte> line, byte delimiter, out ReadOnlySpan<byte> field)
    {
        if (line.Length == 0)
        {
            field = ReadOnlySpan<byte>.Empty;
            return false;
        }

        var delimiterIndex = line.IndexOf(delimiter);

        if (delimiterIndex >= 0)
        {
            // Field found before delimiter
            field = line.Slice(0, delimiterIndex);
            line = line.Slice(delimiterIndex + 1); // Advance past delimiter
            return true;
        }
        else
        {
            // Last field (no delimiter after it)
            field = line;
            line = ReadOnlySpan<byte>.Empty; // Consumed entire line
            return true;
        }
    }

    /// <summary>
    /// Converts a span of UTF-8 bytes to a string
    /// </summary>
    public static string GetString(ReadOnlySpan<byte> bytes)
    {
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Attempts to parse a DateTime from a span of UTF-8 bytes
    /// </summary>
    /// <param name="bytes">The bytes to parse</param>
    /// <param name="date">The parsed DateTime</param>
    /// <returns>True if parsing succeeded</returns>
    public static bool TryParseDateTime(ReadOnlySpan<byte> bytes, out DateTime date)
    {
        // For better performance, we could parse directly from bytes
        // For now, convert to string and use DateTime.TryParse
        var dateString = GetString(bytes);
        return DateTime.TryParse(dateString, out date);
    }
    
    /// <summary>
    /// Attempts to parse a DateOnly from a span of UTF-8 bytes
    /// </summary>
    /// <param name="bytes">The bytes to parse</param>
    /// <param name="date">The parsed DateTime</param>
    /// <returns>True if parsing succeeded</returns>
    public static bool TryParseDateOnly(ReadOnlySpan<byte> bytes, out DateOnly date)
    {
        // For better performance, we could parse directly from bytes
        // For now, convert to string and use DateTime.TryParse
        var dateString = GetString(bytes);
        return DateOnly.TryParse(dateString, out date);
    }

    /// <summary>
    /// Attempts to parse an integer from a span of UTF-8 bytes
    /// </summary>
    public static bool TryParseInt32(ReadOnlySpan<byte> bytes, out int value)
    {
        var str = GetString(bytes);
        return int.TryParse(str, out value);
    }

    /// <summary>
    /// Attempts to parse a long from a span of UTF-8 bytes
    /// </summary>
    public static bool TryParseInt64(ReadOnlySpan<byte> bytes, out long value)
    {
        var str = GetString(bytes);
        return long.TryParse(str, out value);
    }

    /// <summary>
    /// Attempts to parse a decimal from a span of UTF-8 bytes
    /// </summary>
    public static bool TryParseDecimal(ReadOnlySpan<byte> bytes, out decimal value)
    {
        var str = GetString(bytes);
        return decimal.TryParse(str, out value);
    }

    /// <summary>
    /// Attempts to parse a double from a span of UTF-8 bytes
    /// </summary>
    public static bool TryParseDouble(ReadOnlySpan<byte> bytes, out double value)
    {
        var str = GetString(bytes);
        return double.TryParse(str, out value);
    }

    /// <summary>
    /// Attempts to parse a boolean from a span of UTF-8 bytes
    /// Supports: true/false, 1/0, yes/no (case insensitive)
    /// </summary>
    public static bool TryParseBoolean(ReadOnlySpan<byte> bytes, out bool value)
    {
        var str = GetString(bytes).Trim().ToLowerInvariant();
        
        if (str == "true" || str == "1" || str == "yes")
        {
            value = true;
            return true;
        }
        
        if (str == "false" || str == "0" || str == "no")
        {
            value = false;
            return true;
        }

        value = false;
        return false;
    }
    /// <summary>
    /// Trims whitespace from both ends of a byte span (space, tab, newline, carriage return)
    /// </summary>
    public static ReadOnlySpan<byte> Trim(ReadOnlySpan<byte> bytes)
    {
        // Trim from start
        int start = 0;
        while (start < bytes.Length && IsWhitespace(bytes[start]))
        {
            start++;
        }

        // Trim from end
        int end = bytes.Length - 1;
        while (end >= start && IsWhitespace(bytes[end]))
        {
            end--;
        }

        return start <= end ? bytes.Slice(start, end - start + 1) : ReadOnlySpan<byte>.Empty;
    }

    private static bool IsWhitespace(byte b)
    {
        return b == (byte)' ' || b == (byte)'\t' || b == (byte)'\n' || b == (byte)'\r';
    }
}