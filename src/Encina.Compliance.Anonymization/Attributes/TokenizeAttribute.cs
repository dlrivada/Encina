using Encina.Compliance.Anonymization.Model;

namespace Encina.Compliance.Anonymization;

/// <summary>
/// Marks a response property for automatic tokenization by the
/// <see cref="AnonymizationPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
/// <remarks>
/// <para>
/// When the pipeline behavior detects this attribute on a <c>TResponse</c> property,
/// it replaces the property value with a token using the <see cref="ITokenizer"/>. The handler
/// works with real data; tokenization occurs on the way out (response-side transformation).
/// </para>
/// <para>
/// Tokenization differs from pseudonymization in that the token has no mathematical relationship
/// to the original value — it is a random substitute stored in a <see cref="ITokenMappingStore"/>.
/// De-tokenization requires a lookup in the token store, not a cryptographic key.
/// </para>
/// <para>
/// Three token formats are supported:
/// <list type="bullet">
/// <item><see cref="TokenFormat.Uuid"/>: Standard UUID v4 (e.g., <c>"a3f1b2c4-..."</c>)</item>
/// <item><see cref="TokenFormat.Prefixed"/>: UUID with a configurable prefix (e.g., <c>"tok_a3f1b2c4..."</c>)</item>
/// <item><see cref="TokenFormat.FormatPreserving"/>: Preserves the length and character class of the original</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public record PaymentResponse
/// {
///     public Guid Id { get; init; }
///
///     [Tokenize(Format = TokenFormat.FormatPreserving)]
///     public string CreditCardNumber { get; init; } = string.Empty;
///
///     [Tokenize(Format = TokenFormat.Prefixed, Prefix = "acct")]
///     public string BankAccountNumber { get; init; } = string.Empty;
///
///     [Tokenize] // Defaults to UUID format
///     public string TaxId { get; init; } = string.Empty;
///
///     // Non-decorated properties pass through unmodified
///     public decimal Amount { get; init; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class TokenizeAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the token format to use.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="TokenFormat.Uuid"/> (default): Generates a standard UUID v4 token.
    /// Compact and universally unique, but does not preserve the original value's format.
    /// </para>
    /// <para>
    /// <see cref="TokenFormat.Prefixed"/>: Generates a UUID with a configurable
    /// <see cref="Prefix"/> (e.g., <c>"tok_abc123..."</c>). Useful for identifying
    /// tokenized values at a glance.
    /// </para>
    /// <para>
    /// <see cref="TokenFormat.FormatPreserving"/>: Generates a token that preserves
    /// the length and character class (digits, uppercase, lowercase) of the original.
    /// Useful for credit card numbers, phone numbers, and similar structured data.
    /// </para>
    /// </remarks>
    public TokenFormat Format { get; set; } = TokenFormat.Uuid;

    /// <summary>
    /// Gets or sets the prefix for prefixed token format.
    /// </summary>
    /// <remarks>
    /// Only applicable when <see cref="Format"/> is <see cref="TokenFormat.Prefixed"/>.
    /// When <c>null</c> or empty, the default prefix <c>"tok"</c> is used.
    /// The resulting token format is <c>"{prefix}_{uuid}"</c>.
    /// </remarks>
    public string? Prefix { get; set; }
}
