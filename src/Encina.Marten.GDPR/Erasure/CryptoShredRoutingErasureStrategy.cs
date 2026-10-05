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
/// Provenance is first by reference identity: the Marten locator records each location it produces. A location
/// without that marker (copied or rehydrated between locate and erase) whose entity type is or reaches a
/// crypto-shredded owner is crypto-shredded too, and one whose entity type cannot be classified fails, so a
/// Marten location never falls through to a strategy that reports success without erasing it.
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

        return Route(location) switch
        {
            ErasureRoute.CryptoShredding => _cryptoShredding.EraseFieldAsync(location, cancellationToken),
            ErasureRoute.Inner when _inner is not null => _inner.EraseFieldAsync(location, cancellationToken),
            ErasureRoute.Undetermined => ValueTask.FromResult<Either<EncinaError, Unit>>(CryptoShreddingErrors.SerializationError(location.EntityType)),
            _ => ValueTask.FromResult<Either<EncinaError, Unit>>(CryptoShreddingErrors.ErasureStrategyMissing(location.EntityType)),
        };
    }

    /// <summary>
    /// Decides where a location goes. The provenance marker is only a fast path: a copied or rehydrated location
    /// loses it, so a location whose entity type is or reaches a crypto-shredded owner is still crypto-shredded
    /// (fail closed). When the entity type cannot be classified, the location is not erased at all.
    /// </summary>
    internal static ErasureRoute Route(PersonalDataLocation location)
    {
        if (IsMartenLocation(location))
        {
            return ErasureRoute.CryptoShredding;
        }

        try
        {
            return IsCryptoShreddedEntity(location.EntityType) ? ErasureRoute.CryptoShredding : ErasureRoute.Inner;
        }
        catch (Exception ex) when (ex is TypeLoadException or NotSupportedException or ArgumentException or InvalidOperationException)
        {
            return ErasureRoute.Undetermined;
        }
    }

    private static bool IsCryptoShreddedEntity(Type entityType) =>
        CryptoShreddedPropertyClassifier.IsOwner(entityType) || CryptoShreddedPropertyClassifier.ReachesCryptoOwner(entityType);

    /// <summary>Where a location is erased.</summary>
    internal enum ErasureRoute
    {
        /// <summary>Crypto-shredding (delete the subject's keys).</summary>
        CryptoShredding,

        /// <summary>The strategy registered before <c>AddEncinaMartenGdpr</c>.</summary>
        Inner,

        /// <summary>The entity type could not be classified; the location fails.</summary>
        Undetermined,
    }
}
