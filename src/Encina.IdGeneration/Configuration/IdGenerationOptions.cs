namespace Encina.IdGeneration.Configuration;

/// <summary>
/// Top-level configuration for the Encina ID generation package.
/// </summary>
/// <remarks>
/// <para>
/// Use the fluent <c>Use*</c> methods to select which ID generation strategies to register.
/// Multiple strategies can be registered simultaneously for different entity types.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaIdGeneration(options =>
/// {
///     options.UseSnowflake(snowflake =>
///     {
///         snowflake.MachineId = 1;
///     });
///     options.UseUlid();
///     options.UseUuidV7();
/// });
/// </code>
/// </example>
public sealed class IdGenerationOptions
{
    /// <summary>
    /// Gets a value indicating whether the Snowflake ID generator is enabled.
    /// </summary>
    public bool SnowflakeEnabled { get; private set; }

    /// <summary>
    /// Gets the Snowflake generator configuration options.
    /// </summary>
    public SnowflakeOptions SnowflakeOptions { get; } = new();

    /// <summary>
    /// Gets a value indicating whether the ULID generator is enabled.
    /// </summary>
    public bool UlidEnabled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the UUIDv7 generator is enabled.
    /// </summary>
    public bool UuidV7Enabled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the shard-prefixed ID generator is enabled.
    /// </summary>
    public bool ShardPrefixedEnabled { get; private set; }

    /// <summary>
    /// Gets the shard-prefixed generator configuration options.
    /// </summary>
    public ShardPrefixedOptions ShardPrefixedOptions { get; } = new();

    /// <summary>
    /// Enables the Snowflake ID generator with optional configuration.
    /// </summary>
    /// <param name="configure">Optional configuration callback for Snowflake-specific options.</param>
    /// <returns>This <see cref="IdGenerationOptions"/> instance for fluent chaining.</returns>
    public IdGenerationOptions UseSnowflake(Action<SnowflakeOptions>? configure = null)
    {
        SnowflakeEnabled = true;
        configure?.Invoke(SnowflakeOptions);
        return this;
    }

    /// <summary>
    /// Enables the ULID generator.
    /// </summary>
    /// <returns>This <see cref="IdGenerationOptions"/> instance for fluent chaining.</returns>
    public IdGenerationOptions UseUlid()
    {
        UlidEnabled = true;
        return this;
    }

    /// <summary>
    /// Enables the UUIDv7 generator.
    /// </summary>
    /// <returns>This <see cref="IdGenerationOptions"/> instance for fluent chaining.</returns>
    public IdGenerationOptions UseUuidV7()
    {
        UuidV7Enabled = true;
        return this;
    }

    /// <summary>
    /// Enables the shard-prefixed ID generator with optional configuration.
    /// </summary>
    /// <param name="configure">Optional configuration callback for shard-prefixed options.</param>
    /// <returns>This <see cref="IdGenerationOptions"/> instance for fluent chaining.</returns>
    public IdGenerationOptions UseShardPrefixed(Action<ShardPrefixedOptions>? configure = null)
    {
        ShardPrefixedEnabled = true;
        configure?.Invoke(ShardPrefixedOptions);
        return this;
    }
}
