using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.PropertyTests.Compliance.DataResidency;

/// <summary>
/// Property-based tests for <see cref="DefaultCrossBorderTransferValidator"/> verifying
/// GDPR Chapter V transfer invariants across randomized region pairs.
/// </summary>
[Trait("Category", "Property")]
public class DefaultCrossBorderTransferValidatorPropertyTests
{
    /// <summary>
    /// Invariant: Same region transfer is always allowed regardless of region properties.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property SameRegion_AlwaysAllowed()
    {
        var regionGen = Gen.Elements(
            RegionRegistry.DE, RegionRegistry.US, RegionRegistry.CN, RegionRegistry.JP);

        return Prop.ForAll(Arb.From(regionGen), region =>
        {
            var sut = CreateValidator();
            var result = sut.ValidateTransferAsync(
                region, region, "test-data", CancellationToken.None).AsTask().Result;

            return result.Match(
                Right: r => r.IsAllowed,
                Left: _ => false);
        });
    }

    /// <summary>
    /// Invariant: Intra-EEA transfers are always allowed (free movement under GDPR).
    /// </summary>
    [Property(MaxTest = 30)]
    public Property IntraEEA_AlwaysAllowed()
    {
        var eeaGen = Gen.Elements(
            RegionRegistry.DE, RegionRegistry.FR, RegionRegistry.NL,
            RegionRegistry.NO, RegionRegistry.SE);

        var pairGen = Gen.Two(eeaGen);

        return Prop.ForAll(Arb.From(pairGen), pair =>
        {
            var (source, destination) = pair;
            var sut = CreateValidator();
            var result = sut.ValidateTransferAsync(
                source, destination, "test-data", CancellationToken.None).AsTask().Result;

            return result.Match(
                Right: r => r.IsAllowed,
                Left: _ => false);
        });
    }

    private static DefaultCrossBorderTransferValidator CreateValidator()
    {
        var adequacyProvider = new DefaultAdequacyDecisionProvider(
            Options.Create(new DataResidencyOptions()),
            NullLogger<DefaultAdequacyDecisionProvider>.Instance);

        return new DefaultCrossBorderTransferValidator(
            adequacyProvider,
            Options.Create(new DataResidencyOptions()),
            NullLogger<DefaultCrossBorderTransferValidator>.Instance);
    }
}
