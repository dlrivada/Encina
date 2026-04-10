using Encina.Compliance.LawfulBasis;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

using GDPRLawfulBasis = global::Encina.Compliance.GDPR.LawfulBasis;

namespace Encina.PropertyTests.Compliance.LawfulBasis;

/// <summary>
/// Property-based tests for <see cref="LawfulBasisOptionsValidator"/>.
/// </summary>
/// <remarks>
/// These tests instantiate the REAL validator and call Validate() with randomized inputs
/// to exercise the actual validation logic, providing coverage for the property flag.
/// </remarks>
public class LawfulBasisOptionsValidatorPropertyTests
{
    private static readonly Gen<LawfulBasisEnforcementMode> ValidEnforcementModeGen = Gen.Elements(
        LawfulBasisEnforcementMode.Block,
        LawfulBasisEnforcementMode.Warn,
        LawfulBasisEnforcementMode.Disabled);

    private static readonly Gen<GDPRLawfulBasis> ValidBasisGen = Gen.Elements(
        GDPRLawfulBasis.Consent,
        GDPRLawfulBasis.Contract,
        GDPRLawfulBasis.LegalObligation,
        GDPRLawfulBasis.VitalInterests,
        GDPRLawfulBasis.PublicTask,
        GDPRLawfulBasis.LegitimateInterests);

    /// <summary>
    /// Invariant: For any valid enforcement mode, validation should succeed when DefaultBases is empty.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property Validate_ValidEnforcementMode_AlwaysSucceeds()
    {
        return Prop.ForAll(ValidEnforcementModeGen.ToArbitrary(), mode =>
        {
            var validator = new LawfulBasisOptionsValidator();
            var options = new LawfulBasisOptions { EnforcementMode = mode };
            var result = validator.Validate(null, options);
            return result.Succeeded;
        });
    }

    /// <summary>
    /// Invariant: For any invalid (undefined) enforcement mode value, validation must fail.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property Validate_InvalidEnforcementMode_AlwaysFails()
    {
        var invalidGen = ArbMap.Default.GeneratorFor<int>()
            .Where(i => !Enum.IsDefined((LawfulBasisEnforcementMode)i))
            .ToArbitrary();

        return Prop.ForAll(invalidGen, rawValue =>
        {
            var validator = new LawfulBasisOptionsValidator();
            var options = new LawfulBasisOptions
            {
                EnforcementMode = (LawfulBasisEnforcementMode)rawValue
            };
            var result = validator.Validate(null, options);
            return result.Failed;
        });
    }

    /// <summary>
    /// Invariant: Valid default bases in the dictionary never cause validation failures
    /// when the enforcement mode is also valid.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property Validate_ValidDefaultBases_AlwaysSucceeds()
    {
        return Prop.ForAll(ValidBasisGen.ToArbitrary(), basis =>
        {
            var validator = new LawfulBasisOptionsValidator();
            var options = new LawfulBasisOptions();
            options.DefaultBases[typeof(string)] = basis;
            var result = validator.Validate(null, options);
            return result.Succeeded;
        });
    }

    /// <summary>
    /// Invariant: Validation always returns a non-null ValidateOptionsResult.
    /// </summary>
    [Property(MaxTest = 20)]
    public Property Validate_NeverReturnsNull()
    {
        return Prop.ForAll(ValidEnforcementModeGen.ToArbitrary(), mode =>
        {
            var validator = new LawfulBasisOptionsValidator();
            var result = validator.Validate(null, new LawfulBasisOptions { EnforcementMode = mode });
            return result is not null;
        });
    }
}
