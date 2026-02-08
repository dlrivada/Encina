namespace Encina.DomainModeling.Pagination;

/// <summary>
/// Defines the contract for encoding and decoding cursor values.
/// </summary>
/// <remarks>
/// <para>
/// Cursor encoders transform cursor key values (typically composite keys like
/// <c>{ CreatedAt, Id }</c>) into opaque strings and back. The encoded strings
/// are safe to include in URLs and JSON responses.
/// </para>
/// <para>
/// The default implementation <see cref="Base64JsonCursorEncoder"/> uses JSON
/// serialization with Base64 URL-safe encoding.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Encoding a composite cursor
/// var cursor = encoder.Encode(new { CreatedAt = DateTime.UtcNow, Id = 123 });
/// // Result: "eyJDcmVhdGVkQXQiOiIyMDI1LTEyLTI3VDEwOjMwOjAwWiIsIklkIjoxMjN9"
///
/// // Decoding back to the original type
/// var decoded = encoder.Decode&lt;CursorKey&gt;(cursor);
/// </code>
/// </example>
public interface ICursorEncoder
{
    /// <summary>
    /// Encodes a cursor value to an opaque string.
    /// </summary>
    /// <typeparam name="T">The type of the cursor value.</typeparam>
    /// <param name="value">The cursor value to encode. Can be a simple value or composite type.</param>
    /// <returns>
    /// The encoded cursor string, or null if <paramref name="value"/> is null.
    /// </returns>
    /// <remarks>
    /// The encoded string is URL-safe and can be used directly in query parameters.
    /// </remarks>
    string? Encode<T>(T? value);

    /// <summary>
    /// Decodes a cursor string back to its original value.
    /// </summary>
    /// <typeparam name="T">The type to decode to.</typeparam>
    /// <param name="cursor">The encoded cursor string.</param>
    /// <returns>
    /// The decoded cursor value, or default(T) if <paramref name="cursor"/> is null or empty.
    /// </returns>
    /// <exception cref="CursorEncodingException">
    /// Thrown when the cursor string cannot be decoded.
    /// </exception>
    T? Decode<T>(string? cursor);
}

/// <summary>
/// Exception thrown when cursor encoding or decoding fails.
/// </summary>
/// <param name="message">The error message.</param>
/// <param name="innerException">The inner exception, if any.</param>
public sealed class CursorEncodingException(string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    /// <summary>
    /// Creates an exception for an invalid cursor format.
    /// </summary>
    /// <param name="cursor">The invalid cursor string.</param>
    /// <param name="innerException">The inner exception from parsing.</param>
    /// <returns>A new <see cref="CursorEncodingException"/> instance.</returns>
    public static CursorEncodingException InvalidFormat(string cursor, Exception? innerException = null) =>
        new($"The cursor '{cursor}' has an invalid format and cannot be decoded.", innerException);

    /// <summary>
    /// Creates an exception for a cursor that cannot be deserialized to the target type.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="cursor">The cursor string.</param>
    /// <param name="innerException">The inner exception from deserialization.</param>
    /// <returns>A new <see cref="CursorEncodingException"/> instance.</returns>
    public static CursorEncodingException DeserializationFailed<T>(string cursor, Exception? innerException = null) =>
        new($"The cursor '{cursor}' could not be deserialized to type '{typeof(T).Name}'.", innerException);
}
