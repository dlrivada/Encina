using System.Runtime.CompilerServices;

using Encina.Compliance.DataSubjectRights;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.Marten.GDPR;

/// <summary>
/// The <see cref="IDataErasureStrategy"/> that <c>AddEncinaMartenGdpr</c> registers: locations produced by
/// <see cref="MartenEventPersonalDataLocator"/> go to <see cref="CryptoShredErasureStrategy"/>; every other
/// location goes to the strategy that was registered before (its inner strategy), or fails with
/// <c>crypto.erasure_strategy_missing</c> when there is none.
/// </summary>
/// <remarks>
/// <para>
/// The data subject rights executor resolves one strategy and applies it to every location. Without this router a
/// custom strategy would receive Marten crypto-shredded locations and report success while the data stays
/// decryptable (Art. 17 fail-open), and crypto-shredding would report success for locations of other locators.
/// </para>
/// <para>
/// Provenance is by reference identity: the Marten locator records each location it produces, and the executor
/// passes those instances unchanged.
/// </para>
/// </remarks>
internal sealed class CryptoShredRoutingErasureStrategy : IDataErasureStrategy
{
    /// <summary>The service key of the strategy registered before <c>AddEncinaMartenGdpr</c>.</summary>
    internal const string InnerStrategyKey = "Encina.Marten.GDPR.InnerErasureStrategy";

    private static readonly ConditionalWeakTable<PersonalDataLocation, object> MartenLocations = new();
    private static readonly object Marker = new();

    private readonly CryptoShredErasureStrategy _cryptoShredding;
    private readonly IDataErasureStrategy? _inner;

    public CryptoShredRoutingErasureStrategy(
        CryptoShredErasureStrategy cryptoShredding,
        [FromKeyedServices(InnerStrategyKey)] IEnumerable<IDataErasureStrategy> inner)
    {
        _cryptoShredding = cryptoShredding;
        _inner = inner.LastOrDefault();
    }

    /// <summary>Records that the Marten locator produced <paramref name="location"/>.</summary>
    internal static void MarkMartenLocation(PersonalDataLocation location) => MartenLocations.AddOrUpdate(location, Marker);

    /// <summary>Gets whether the Marten locator produced <paramref name="location"/>.</summary>
    internal static bool IsMartenLocation(PersonalDataLocation location) => MartenLocations.TryGetValue(location, out _);

    /// <summary>Gets the strategy registered before <c>AddEncinaMartenGdpr</c>, or <c>null</c>.</summary>
    internal IDataErasureStrategy? Inner => _inner;

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> EraseFieldAsync(
        PersonalDataLocation location,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        if (IsMartenLocation(location))
        {
            return _cryptoShredding.EraseFieldAsync(location, cancellationToken);
        }

        return _inner is null
            ? ValueTask.FromResult<Either<EncinaError, Unit>>(CryptoShreddingErrors.ErasureStrategyMissing(location.EntityType))
            : _inner.EraseFieldAsync(location, cancellationToken);
    }
}
