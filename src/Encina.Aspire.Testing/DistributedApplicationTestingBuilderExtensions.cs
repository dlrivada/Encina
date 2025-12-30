using System.Linq;
using Aspire.Hosting.Testing;
using Encina.Testing.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Aspire.Testing;

/// <summary>
/// Extension methods for <see cref="IDistributedApplicationTestingBuilder"/> to add Encina test support.
/// </summary>
public static class DistributedApplicationTestingBuilderExtensions
{
    /// <summary>
    /// Adds Encina test support to the distributed application testing builder.
    /// </summary>
    /// <param name="builder">The distributed application testing builder.</param>
    /// <param name="configure">Optional action to configure test support options.</param>
    /// <returns>The builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when this method has already been called on the builder.</exception>
    /// <remarks>
    /// <para>
    /// This method registers fake implementations of all Encina messaging stores
    /// and configures the test environment for integration testing.
    /// </para>
    /// <para>
    /// By default, all messaging stores are cleared before each test. This behavior
    /// can be customized through the <paramref name="configure"/> action.
    /// </para>
    /// <para>
    /// This method may only be called once per builder. Subsequent calls will throw
    /// an <see cref="InvalidOperationException"/> to prevent configuration conflicts.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var builder = await DistributedApplicationTestingBuilder
    ///     .CreateAsync&lt;Projects.MyAppHost&gt;();
    ///
    /// builder.WithEncinaTestSupport(options =>
    /// {
    ///     options.ClearOutboxBeforeTest = true;
    ///     options.DefaultWaitTimeout = TimeSpan.FromSeconds(60);
    /// });
    ///
    /// await using var app = await builder.BuildAsync();
    /// </code>
    /// </example>
    public static IDistributedApplicationTestingBuilder WithEncinaTestSupport(
        this IDistributedApplicationTestingBuilder builder,
        Action<EncinaTestSupportOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Check if already registered - throw to prevent silent configuration conflicts
        if (builder.Services.Any(sd => sd.ServiceType == typeof(EncinaTestSupportOptions)))
        {
            throw new InvalidOperationException(
                "WithEncinaTestSupport may only be called once per builder. " +
                "Encina test support has already been configured.");
        }

        var options = new EncinaTestSupportOptions();
        configure?.Invoke(options);

        // Register options as singleton
        builder.Services.AddSingleton(options);

        // Register fake stores for testing (already idempotent via TryAddSingleton internally)
        builder.Services.AddEncinaFakes();

        // Register the test context
        builder.Services.AddSingleton<EncinaTestContext>();

        return builder;
    }
}
