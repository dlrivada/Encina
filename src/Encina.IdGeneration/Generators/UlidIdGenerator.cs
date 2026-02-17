using LanguageExt;

namespace Encina.IdGeneration.Generators;

/// <summary>
/// Generates ULID (Universally Unique Lexicographically Sortable Identifier) values
/// using cryptographic randomness.
/// </summary>
/// <remarks>
/// <para>
/// ULIDs encode a 48-bit Unix timestamp (millisecond precision) in the first 6 bytes,
/// followed by 10 bytes (80 bits) of cryptographically random data. This produces
/// 128-bit identifiers that are time-ordered and globally unique.
/// </para>
/// <para>
/// This generator delegates to <see cref="UlidId.NewUlid(DateTimeOffset)"/>, which uses
/// <see cref="System.Security.Cryptography.RandomNumberGenerator"/> for the random portion.
/// </para>
/// <para>
/// Thread-safety: This generator is inherently thread-safe because each call produces
/// an independent ULID with fresh random bytes. No shared mutable state is maintained.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var generator = new UlidIdGenerator();
/// var result = generator.Generate(); // Either&lt;EncinaError, UlidId&gt;
/// </code>
/// </example>
public sealed class UlidIdGenerator : IIdGenerator<UlidId>
{
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="UlidIdGenerator"/> class.
    /// </summary>
    /// <param name="timeProvider">
    /// Optional time provider for testable time operations. Defaults to <see cref="TimeProvider.System"/>.
    /// </param>
    public UlidIdGenerator(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public string StrategyName => "ULID";

    /// <inheritdoc />
    public Either<EncinaError, UlidId> Generate()
    {
        var timestamp = _timeProvider.GetUtcNow();
        return UlidId.NewUlid(timestamp);
    }
}
