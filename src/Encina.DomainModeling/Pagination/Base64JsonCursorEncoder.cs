using System.Buffers;
using System.Text.Json;

namespace Encina.DomainModeling.Pagination;

/// <summary>
/// Default implementation of <see cref="ICursorEncoder"/> using JSON serialization
/// with Base64 URL-safe encoding.
/// </summary>
/// <remarks>
/// <para>
/// This encoder serializes cursor values to JSON using <see cref="System.Text.Json"/>,
/// then encodes the JSON bytes to a URL-safe Base64 string. The encoding uses the
/// base64url alphabet (RFC 4648 Section 5) which replaces '+' with '-' and '/' with '_',
/// and omits padding characters.
/// </para>
/// <para>
/// Composite cursor values (like anonymous types with multiple properties) are fully
/// supported. The JSON representation preserves property names and types for accurate
/// round-trip serialization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var encoder = new Base64JsonCursorEncoder();
///
/// // Simple value
/// var cursor1 = encoder.Encode(42);
/// // Result: "NDI"
///
/// // Composite cursor (typical usage)
/// var cursor2 = encoder.Encode(new { CreatedAt = DateTime.UtcNow, Id = 123 });
/// // Result: "eyJDcmVhdGVkQXQiOiIyMDI1LTEyLTI3VDEwOjMwOjAwWiIsIklkIjoxMjN9"
///
/// // Decoding
/// var decoded = encoder.Decode&lt;int&gt;(cursor1);
/// // Result: 42
/// </code>
/// </example>
public sealed class Base64JsonCursorEncoder : ICursorEncoder
{
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="Base64JsonCursorEncoder"/> class
    /// with default JSON serialization options.
    /// </summary>
    public Base64JsonCursorEncoder()
        : this(CreateDefaultOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Base64JsonCursorEncoder"/> class
    /// with custom JSON serialization options.
    /// </summary>
    /// <param name="jsonOptions">The JSON serialization options to use.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="jsonOptions"/> is null.
    /// </exception>
    public Base64JsonCursorEncoder(JsonSerializerOptions jsonOptions)
    {
        ArgumentNullException.ThrowIfNull(jsonOptions);
        _jsonOptions = jsonOptions;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The encoding process:
    /// <list type="number">
    /// <item><description>Serialize the value to JSON bytes using System.Text.Json</description></item>
    /// <item><description>Encode the bytes to Base64 URL-safe format (RFC 4648 Section 5)</description></item>
    /// <item><description>Remove padding characters ('=') for a cleaner URL</description></item>
    /// </list>
    /// </remarks>
    public string? Encode<T>(T? value)
    {
        if (value is null)
        {
            return null;
        }

        try
        {
            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(value, _jsonOptions);
            return Base64UrlEncode(jsonBytes);
        }
        catch (JsonException ex)
        {
            throw new CursorEncodingException(
                $"Failed to serialize cursor value of type '{typeof(T).Name}' to JSON.",
                ex);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// The decoding process:
    /// <list type="number">
    /// <item><description>Decode the Base64 URL-safe string to bytes</description></item>
    /// <item><description>Deserialize the JSON bytes to the target type</description></item>
    /// </list>
    /// </remarks>
    public T? Decode<T>(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor))
        {
            return default;
        }

        byte[] jsonBytes;
        try
        {
            jsonBytes = Base64UrlDecode(cursor);
        }
        catch (FormatException ex)
        {
            throw CursorEncodingException.InvalidFormat(cursor, ex);
        }

        try
        {
            return JsonSerializer.Deserialize<T>(jsonBytes, _jsonOptions);
        }
        catch (JsonException ex)
        {
            throw CursorEncodingException.DeserializationFailed<T>(cursor, ex);
        }
    }

    /// <summary>
    /// Encodes bytes to a Base64 URL-safe string (RFC 4648 Section 5).
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The Base64 URL-safe encoded string without padding.</returns>
    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes)
    {
        // Calculate the required length for Base64 encoding
        var base64Length = ((bytes.Length + 2) / 3) * 4;

        // Rent a buffer from the pool for efficiency
        var buffer = ArrayPool<char>.Shared.Rent(base64Length);
        try
        {
            // Convert to standard Base64
            var written = Convert.ToBase64CharArray(
                bytes.ToArray(),
                0,
                bytes.Length,
                buffer,
                0);

            // Convert to URL-safe Base64 and trim padding
            var result = new char[written];
            var resultLength = 0;

            for (var i = 0; i < written; i++)
            {
                var c = buffer[i];
                switch (c)
                {
                    case '+':
                        result[resultLength++] = '-';
                        break;
                    case '/':
                        result[resultLength++] = '_';
                        break;
                    case '=':
                        // Skip padding
                        break;
                    default:
                        result[resultLength++] = c;
                        break;
                }
            }

            return new string(result, 0, resultLength);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Decodes a Base64 URL-safe string (RFC 4648 Section 5) to bytes.
    /// </summary>
    /// <param name="base64Url">The Base64 URL-safe encoded string.</param>
    /// <returns>The decoded bytes.</returns>
    /// <exception cref="FormatException">
    /// Thrown when the input is not a valid Base64 URL-safe string.
    /// </exception>
    private static byte[] Base64UrlDecode(string base64Url)
    {
        // Calculate padding needed
        var paddingNeeded = (4 - (base64Url.Length % 4)) % 4;

        // Rent a buffer for the conversion
        var bufferLength = base64Url.Length + paddingNeeded;
        var buffer = ArrayPool<char>.Shared.Rent(bufferLength);
        try
        {
            // Convert URL-safe Base64 to standard Base64
            for (var i = 0; i < base64Url.Length; i++)
            {
                buffer[i] = base64Url[i] switch
                {
                    '-' => '+',
                    '_' => '/',
                    _ => base64Url[i]
                };
            }

            // Add padding
            for (var i = 0; i < paddingNeeded; i++)
            {
                buffer[base64Url.Length + i] = '=';
            }

            return Convert.FromBase64CharArray(buffer, 0, bufferLength);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Creates the default JSON serialization options for cursor encoding.
    /// </summary>
    /// <returns>The default JSON serialization options.</returns>
    private static JsonSerializerOptions CreateDefaultOptions() => new()
    {
        // Use camelCase for consistency with JavaScript/JSON conventions
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

        // Be lenient when reading (accept PascalCase even though we write camelCase)
        PropertyNameCaseInsensitive = true,

        // Include all properties, even with default values
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,

        // Strict number handling
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict,

        // Write minimal JSON (no extra whitespace)
        WriteIndented = false
    };
}
