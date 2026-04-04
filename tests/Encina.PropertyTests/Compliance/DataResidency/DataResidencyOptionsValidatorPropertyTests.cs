using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

using Microsoft.Extensions.Options;

namespace Encina.PropertyTests.Compliance.DataResidency;

/// <summary>
/// Property-based tests for <see cref="DataResidencyOptionsValidator"/> verifying
/// invariants hold across random valid and invalid configurations.
/// </summary>
[Trait("Category", "Property")]
public class DataResidencyOptionsValidatorPropertyTests
{
    private readonly DataResidencyOptionsValidator _sut = new();

    /// <summary>
    /// Invariant: When EnforcementMode is Block, validation only succeeds if DefaultRegion is set.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property BlockMode_RequiresDefaultRegion()
    {
        return Prop.ForAll(
            Arb.From(Gen.OneOf(
                Gen.Constant<Region?>(null),
                Gen.Constant<Region?>(RegionRegistry.DE),
                Gen.Constant<Region?>(RegionRegistry.FR))),
            region =>
            {
                var options = new DataResidencyOptions
                {
                    EnforcementMode = DataResidencyEnforcementMode.Block,
                    DefaultRegion = region
                };
                var result = _sut.Validate(null, options);

                if (region is not null)
                    return result.Succeeded;
                else
                    return result.Failed;
            });
    }

    /// <summary>
    /// Invariant: Disabled and Warn modes always succeed regardless of DefaultRegion.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property NonBlockModes_AlwaysSucceedWithoutRegion()
    {
        var modeGen = Gen.Elements(
            DataResidencyEnforcementMode.Warn,
            DataResidencyEnforcementMode.Disabled);

        return Prop.ForAll(Arb.From(modeGen), mode =>
        {
            var options = new DataResidencyOptions
            {
                EnforcementMode = mode,
                DefaultRegion = null
            };
            return _sut.Validate(null, options).Succeeded;
        });
    }
}
