using Encina.Compliance.Anonymization;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.Anonymization;

/// <summary>
/// Property-based tests for <see cref="AnonymizationOptionsValidator"/> verifying
/// invariants hold across random valid and invalid configurations.
/// </summary>
[Trait("Category", "Property")]
public class AnonymizationOptionsValidatorPropertyTests
{
    private readonly AnonymizationOptionsValidator _sut = new();

    /// <summary>
    /// Invariant: Any defined <see cref="AnonymizationEnforcementMode"/> value always validates successfully.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property ValidEnforcementMode_AlwaysSucceeds()
    {
        var validModeGen = Gen.Elements(
            AnonymizationEnforcementMode.Block,
            AnonymizationEnforcementMode.Warn,
            AnonymizationEnforcementMode.Disabled);

        return Prop.ForAll(Arb.From(validModeGen), mode =>
        {
            var options = new AnonymizationOptions { EnforcementMode = mode };
            return _sut.Validate(null, options).Succeeded;
        });
    }

    /// <summary>
    /// Invariant: An undefined enum value for EnforcementMode always fails validation.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property InvalidEnforcementMode_AlwaysFails()
    {
        // Generate integers outside the valid enum range (0, 1, 2)
        var invalidGen = Gen.Choose(3, 100)
            .Select(i => (AnonymizationEnforcementMode)i);

        return Prop.ForAll(Arb.From(invalidGen), mode =>
        {
            var options = new AnonymizationOptions { EnforcementMode = mode };
            return _sut.Validate(null, options).Failed;
        });
    }
}
