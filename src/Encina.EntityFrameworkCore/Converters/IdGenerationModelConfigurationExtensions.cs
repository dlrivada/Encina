using Encina.IdGeneration;
using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.Converters;

/// <summary>
/// Extension methods for configuring ID generation type conventions in EF Core models.
/// </summary>
/// <remarks>
/// <para>
/// Call <see cref="ConfigureIdGenerationConventions"/> in your <c>DbContext.ConfigureConventions</c>
/// override to automatically apply the correct value converters for all Encina ID types.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
/// {
///     configurationBuilder.ConfigureIdGenerationConventions();
/// }
/// </code>
/// </example>
public static class IdGenerationModelConfigurationExtensions
{
    /// <summary>
    /// Configures EF Core conventions for all Encina ID generation types.
    /// </summary>
    /// <param name="configurationBuilder">The model configuration builder.</param>
    /// <returns>The same <see cref="ModelConfigurationBuilder"/> for chaining.</returns>
    public static ModelConfigurationBuilder ConfigureIdGenerationConventions(
        this ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        configurationBuilder.Properties<SnowflakeId>()
            .HaveConversion<SnowflakeIdConverter>();

        configurationBuilder.Properties<UlidId>()
            .HaveConversion<UlidIdConverter>();

        configurationBuilder.Properties<UuidV7Id>()
            .HaveConversion<UuidV7IdConverter>();

        configurationBuilder.Properties<ShardPrefixedId>()
            .HaveConversion<ShardPrefixedIdConverter>();

        return configurationBuilder;
    }
}
