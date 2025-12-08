using System.Text;
using LegalAnalysisAPI.Domain;

namespace LegalAnalysisAPI.Infrastructure;

/// <summary>
/// Encodes and decodes IDs with type prefixes for API exposure.
/// Uses Base64 URL-safe encoding.
/// </summary>
public interface IIdEncoder
{
    /// <summary>
    /// Encodes an integer ID with a type prefix.
    /// </summary>
    string EncodeInt(int id, string prefix);
    
    /// <summary>
    /// Encodes a long ID with a type prefix.
    /// </summary>
    string EncodeLong(long id, string prefix);
    
    /// <summary>
    /// Encodes a Guid ID with a type prefix.
    /// </summary>
    string EncodeGuid(Guid id, string prefix);
    
    /// <summary>
    /// Decodes an encoded ID to an integer.
    /// </summary>
    Result<int, Error> DecodeInt(string encodedId, string expectedPrefix);
    
    /// <summary>
    /// Decodes an encoded ID to a long.
    /// </summary>
    Result<long, Error> DecodeLong(string encodedId, string expectedPrefix);
    
    /// <summary>
    /// Decodes an encoded ID to a Guid.
    /// </summary>
    Result<Guid, Error> DecodeGuid(string encodedId, string expectedPrefix);
}

public class IdEncoder : IIdEncoder
{
    public string EncodeInt(int id, string prefix)
    {
        var raw = $"{prefix}_{id}";
        var bytes = Encoding.UTF8.GetBytes(raw);
        return Base64UrlEncode(bytes);
    }
    
    public string EncodeLong(long id, string prefix)
    {
        var raw = $"{prefix}_{id}";
        var bytes = Encoding.UTF8.GetBytes(raw);
        return Base64UrlEncode(bytes);
    }
    
    public string EncodeGuid(Guid id, string prefix)
    {
        var raw = $"{prefix}_{id}";
        var bytes = Encoding.UTF8.GetBytes(raw);
        return Base64UrlEncode(bytes);
    }
    
    public Result<int, Error> DecodeInt(string encodedId, string expectedPrefix)
    {
        var decodeResult = DecodeRaw(encodedId, expectedPrefix);
        
        if (decodeResult is Result<string, Error>.Failure(var error))
            return new Result<int, Error>.Failure(error);
        
        var rawValue = ((Result<string, Error>.Success)decodeResult).Value;
        
        if (!int.TryParse(rawValue, out var id))
            return new Result<int, Error>.Failure(
                new Error.ValidationError($"Invalid ID format: cannot parse '{rawValue}' as integer"));
        
        if (id <= 0)
            return new Result<int, Error>.Failure(
                new Error.ValidationError("ID must be greater than zero"));
        
        return new Result<int, Error>.Success(id);
    }
    
    public Result<long, Error> DecodeLong(string encodedId, string expectedPrefix)
    {
        var decodeResult = DecodeRaw(encodedId, expectedPrefix);
        
        if (decodeResult is Result<string, Error>.Failure(var error))
            return new Result<long, Error>.Failure(error);
        
        var rawValue = ((Result<string, Error>.Success)decodeResult).Value;
        
        if (!long.TryParse(rawValue, out var id))
            return new Result<long, Error>.Failure(
                new Error.ValidationError($"Invalid ID format: cannot parse '{rawValue}' as long"));
        
        if (id <= 0)
            return new Result<long, Error>.Failure(
                new Error.ValidationError("ID must be greater than zero"));
        
        return new Result<long, Error>.Success(id);
    }
    
    public Result<Guid, Error> DecodeGuid(string encodedId, string expectedPrefix)
    {
        var decodeResult = DecodeRaw(encodedId, expectedPrefix);
        
        if (decodeResult is Result<string, Error>.Failure(var error))
            return new Result<Guid, Error>.Failure(error);
        
        var rawValue = ((Result<string, Error>.Success)decodeResult).Value;
        
        if (!Guid.TryParse(rawValue, out var id))
            return new Result<Guid, Error>.Failure(
                new Error.ValidationError($"Invalid ID format: cannot parse '{rawValue}' as Guid"));
        
        if (id == Guid.Empty)
            return new Result<Guid, Error>.Failure(
                new Error.ValidationError("ID cannot be empty"));
        
        return new Result<Guid, Error>.Success(id);
    }
    
    private Result<string, Error> DecodeRaw(string encodedId, string expectedPrefix)
    {
        if (string.IsNullOrWhiteSpace(encodedId))
            return new Result<string, Error>.Failure(
                new Error.ValidationError("Encoded ID cannot be empty"));
        
        byte[] bytes;
        try
        {
            bytes = Base64UrlDecode(encodedId);
        }
        catch
        {
            return new Result<string, Error>.Failure(
                new Error.ValidationError("Invalid encoded ID format"));
        }
        
        string raw;
        try
        {
            raw = Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return new Result<string, Error>.Failure(
                new Error.ValidationError("Invalid encoded ID format"));
        }
        
        var parts = raw.Split('_', 2);
        if (parts.Length != 2)
            return new Result<string, Error>.Failure(
                new Error.ValidationError("Invalid ID format: missing prefix"));
        
        var prefix = parts[0];
        var value = parts[1];
        
        if (prefix != expectedPrefix)
            return new Result<string, Error>.Failure(
                new Error.ValidationError($"Invalid ID type: expected '{expectedPrefix}' but got '{prefix}'"));
        
        return new Result<string, Error>.Success(value);
    }
    
    private static string Base64UrlEncode(byte[] input)
    {
        var base64 = Convert.ToBase64String(input);
        // Make URL-safe: replace + with -, / with _, and remove padding =
        return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
    
    private static byte[] Base64UrlDecode(string input)
    {
        // Reverse URL-safe encoding
        var base64 = input.Replace('-', '+').Replace('_', '/');
        
        // Add padding if needed
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        
        return Convert.FromBase64String(base64);
    }
}