using Encina.Compliance.Anonymization.Model;

using LanguageExt;

namespace Encina.Compliance.Anonymization;

/// <summary>
/// Service for replacing sensitive values with non-sensitive tokens and resolving them back.
/// </summary>
/// <remarks>
/// <para>
/// Tokenization substitutes a sensitive data element with a non-sensitive equivalent (token)
/// that has no exploitable meaning or value. Unlike pseudonymization, tokenization does not
/// use cryptographic algorithms on the data itself — instead, it maintains a lookup table
/// (<see cref="ITokenMappingStore"/>) mapping tokens to their original encrypted values.
/// </para>
/// <para>
/// Tokenization is commonly used for:
/// <list type="bullet">
/// <item>Credit card numbers (PCI DSS compliance)</item>
/// <item>Social security numbers</item>
/// <item>Medical record numbers</item>
/// <item>Any value where format-preservation is important</item>
/// </list>
/// </para>
/// <para>
/// Three token formats are supported:
/// <list type="bullet">
/// <item>
/// <term><see cref="TokenFormat.Uuid"/></term>
/// <description>Standard UUID v4 token (e.g., <c>a1b2c3d4-e5f6-7890-abcd-ef1234567890</c>)</description>
/// </item>
/// <item>
/// <term><see cref="TokenFormat.Prefixed"/></term>
/// <description>UUID with a configurable prefix (e.g., <c>tok_a1b2c3d4...</c>)</description>
/// </item>
/// <item>
/// <term><see cref="TokenFormat.FormatPreserving"/></term>
/// <description>Token preserving the format of the original value (e.g., 16-digit number for credit cards)</description>
/// </item>
/// </list>
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Tokenize a credit card number
/// var options = new TokenizationOptions
/// {
///     Format = TokenFormat.Prefixed,
///     Prefix = "cc_"
/// };
///
/// var tokenResult = await tokenizer.TokenizeAsync("4111-1111-1111-1111", options, cancellationToken);
/// // tokenResult => Right("cc_a1b2c3d4-e5f6-7890-abcd-ef1234567890")
///
/// // Resolve the token back to the original value
/// var originalResult = await tokenizer.DetokenizeAsync("cc_a1b2c3d4-e5f6-7890-abcd-ef1234567890", cancellationToken);
/// // originalResult => Right("4111-1111-1111-1111")
/// </code>
/// </example>
public interface ITokenizer
{
    /// <summary>
    /// Replaces a sensitive value with a non-sensitive token.
    /// </summary>
    /// <param name="value">The sensitive value to tokenize.</param>
    /// <param name="options">Options controlling the token format, prefix, and length preservation.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The generated token string, or an <see cref="EncinaError"/> if tokenization failed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If the same <paramref name="value"/> has already been tokenized, the existing token
    /// is returned (deduplication via <see cref="ITokenMappingStore"/>). This ensures
    /// referential integrity across tokenized datasets.
    /// </para>
    /// <para>
    /// The original value is encrypted and stored in the <see cref="ITokenMappingStore"/>
    /// for later detokenization.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, string>> TokenizeAsync(
        string value,
        TokenizationOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a token back to its original sensitive value.
    /// </summary>
    /// <param name="token">The token to resolve.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The original value, or an <see cref="EncinaError"/> if the token was not found
    /// in the <see cref="ITokenMappingStore"/> or decryption failed.
    /// </returns>
    /// <remarks>
    /// Detokenization requires the token mapping to exist in the store and the corresponding
    /// cryptographic key to be available via <see cref="IKeyProvider"/>.
    /// </remarks>
    ValueTask<Either<EncinaError, string>> DetokenizeAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a given value is a known token in the mapping store.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if the value exists as a token in the <see cref="ITokenMappingStore"/>,
    /// <c>false</c> otherwise, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Useful for determining whether a field value has already been tokenized,
    /// preventing double-tokenization.
    /// </remarks>
    ValueTask<Either<EncinaError, bool>> IsTokenAsync(
        string value,
        CancellationToken cancellationToken = default);
}
