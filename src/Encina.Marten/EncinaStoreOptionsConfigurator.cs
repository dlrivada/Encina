using global::Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Encina.Marten;

/// <summary>
/// Bridges the Microsoft Options pattern to Marten's own configuration hook.
/// </summary>
/// <remarks>
/// <para>
/// Encina packages configure Marten's <see cref="StoreOptions"/> by registering
/// <see cref="IConfigureOptions{StoreOptions}"/> (event metadata columns, upcasters,
/// crypto-shredding serializer, audit projections). Marten does not use the Options pattern
/// when it builds its store: it applies only the registered <see cref="IConfigureMarten"/>
/// services. Without this bridge those configurators are never executed (issue #1096).
/// </para>
/// <para>
/// The bridge runs every <see cref="IConfigureOptions{StoreOptions}"/> in registration order,
/// then every <see cref="IPostConfigureOptions{StoreOptions}"/>, against the
/// <see cref="StoreOptions"/> instance Marten is about to use.
/// </para>
/// </remarks>
internal sealed class EncinaStoreOptionsConfigurator : IConfigureMarten
{
    /// <inheritdoc />
    public void Configure(IServiceProvider services, StoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        foreach (var configure in services.GetServices<IConfigureOptions<StoreOptions>>())
        {
            configure.Configure(options);
        }

        foreach (var postConfigure in services.GetServices<IPostConfigureOptions<StoreOptions>>())
        {
            postConfigure.PostConfigure(Options.DefaultName, options);
        }
    }
}
