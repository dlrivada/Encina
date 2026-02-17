using LanguageExt;

namespace Encina.IdGeneration.Generators;

/// <summary>
/// Generates UUID Version 7 (RFC 9562) identifiers using cryptographic randomness.
/// </summary>
/// <remarks>
/// <para>
/// UUIDv7 encodes a 48-bit Unix timestamp in the most significant bits, followed by
/// a version nibble (0111), random data, and the RFC 4122 variant bits. This produces
/// time-ordered UUIDs suitable for database primary keys with B-tree index locality.
/// </para>
/// <para>
/// This generator delegates to <see cref="UuidV7Id.NewUuidV7(DateTimeOffset)"/>, which uses
/// <see cref="System.Security.Cryptography.RandomNumberGenerator"/> for the random portion
/// and sets version/variant bits per RFC 9562.
/// </para>
/// <para>
/// Thread-safety: This generator is inherently thread-safe because each call produces
/// an independent UUIDv7 with fresh random bytes. No shared mutable state is maintained.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var generator = new UuidV7IdGenerator();
/// var result = generator.Generate(); // Either&lt;EncinaError, UuidV7Id&gt;
/// </code>
/// </example>
public sealed class UuidV7IdGenerator : IIdGenerator<UuidV7Id>
{
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="UuidV7IdGenerator"/> class.
    /// </summary>
    /// <param name="timeProvider">
    /// Optional time provider for testable time operations. Defaults to <see cref="TimeProvider.System"/>.
    /// </param>
    public UuidV7IdGenerator(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public string StrategyName => "UUIDv7";

    /// <inheritdoc />
    public Either<EncinaError, UuidV7Id> Generate()
    {
        var timestamp = _timeProvider.GetUtcNow();
        return UuidV7Id.NewUuidV7(timestamp);
    }
}
