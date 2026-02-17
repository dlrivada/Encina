using LanguageExt;

namespace Encina;

/// <summary>
/// Non-generic base interface for distributed ID generators.
/// </summary>
/// <remarks>
/// <para>
/// This marker interface allows registration and discovery of ID generators
/// without knowing the specific ID type at compile time. For typed generation,
/// use <see cref="IIdGenerator{TId}"/>.
/// </para>
/// </remarks>
public interface IIdGenerator
{
    /// <summary>
    /// Gets the name of the generation strategy (e.g., "Snowflake", "ULID", "UUIDv7", "ShardPrefixed").
    /// </summary>
    string StrategyName { get; }
}

/// <summary>
/// Generates unique distributed identifiers of type <typeparamref name="TId"/>.
/// </summary>
/// <typeparam name="TId">The strongly-typed ID value type produced by this generator.</typeparam>
/// <remarks>
/// <para>
/// All generation operations return <see cref="Either{EncinaError, TId}"/> following
/// the Railway Oriented Programming pattern. Generation can fail due to clock drift
/// (Snowflake), sequence exhaustion, or other infrastructure issues.
/// </para>
/// <para>
/// Implementations are expected to be thread-safe and suitable for high-throughput
/// concurrent generation (target: &gt;1M IDs/sec).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Inject and use a Snowflake ID generator
/// public class CreateOrderHandler(IIdGenerator&lt;SnowflakeId&gt; idGenerator)
/// {
///     public Either&lt;EncinaError, Order&gt; Handle(CreateOrder command)
///     {
///         return idGenerator.Generate()
///             .Map(id => new Order(id, command.CustomerId));
///     }
/// }
/// </code>
/// </example>
public interface IIdGenerator<TId> : IIdGenerator
{
    /// <summary>
    /// Generates a new unique identifier.
    /// </summary>
    /// <returns>
    /// Right with the generated ID; Left with an <see cref="EncinaError"/> if generation
    /// fails (e.g., clock drift detected, sequence exhausted).
    /// </returns>
    Either<EncinaError, TId> Generate();
}
