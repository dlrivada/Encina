namespace Encina.Compliance.Anonymization.Model;

/// <summary>
/// Configuration for a tokenization operation, controlling the format and behavior of generated tokens.
/// </summary>
/// <remarks>
/// <para>
/// Tokenization replaces sensitive values with non-sensitive substitutes (tokens).
/// Unlike pseudonymization, tokens have no cryptographic or mathematical relationship
/// to the original value — the mapping is maintained externally in an <c>ITokenMappingStore</c>.
/// </para>
/// <para>
/// The token format affects storage requirements, readability, and compatibility with
/// downstream systems. Choose <see cref="TokenFormat.FormatPreserving"/> when external
/// systems validate field formats (e.g., credit card Luhn check).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // UUID-based token (default)
/// var uuidOptions = new TokenizationOptions { Format = TokenFormat.Uuid };
///
/// // Prefixed token for customer IDs
/// var prefixedOptions = new TokenizationOptions
/// {
///     Format = TokenFormat.Prefixed,
///     Prefix = "cust"
/// };
///
/// // Format-preserving token for phone numbers
/// var fpOptions = new TokenizationOptions
/// {
///     Format = TokenFormat.FormatPreserving,
///     PreserveLength = true
/// };
/// </code>
/// </example>
public sealed record TokenizationOptions
{
    /// <summary>
    /// The format to use for generated tokens.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="TokenFormat.Uuid"/> for maximum uniqueness and simplicity.
    /// </remarks>
    public required TokenFormat Format { get; init; }

    /// <summary>
    /// Optional prefix for tokens when <see cref="Format"/> is <see cref="TokenFormat.Prefixed"/>.
    /// </summary>
    /// <remarks>
    /// The prefix is prepended with an underscore separator (e.g., prefix "usr" produces tokens
    /// like "usr_a1b2c3d4"). Ignored for non-prefixed formats. Maximum recommended length is
    /// 10 characters.
    /// </remarks>
    public string? Prefix { get; init; }

    /// <summary>
    /// Whether to preserve the character length of the original value in the generated token.
    /// </summary>
    /// <remarks>
    /// Only applicable when <see cref="Format"/> is <see cref="TokenFormat.FormatPreserving"/>.
    /// When <c>true</c>, the token will have the same number of characters as the original value.
    /// Defaults to <c>false</c>.
    /// </remarks>
    public bool PreserveLength { get; init; }
}
