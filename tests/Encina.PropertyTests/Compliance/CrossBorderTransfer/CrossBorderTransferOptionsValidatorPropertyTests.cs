using Encina.Compliance.CrossBorderTransfer;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

using Microsoft.Extensions.Options;

namespace Encina.PropertyTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Property-based tests for <see cref="CrossBorderTransferOptionsValidator"/>
/// verifying validation invariants across randomized inputs using FsCheck.
/// </summary>
[Trait("Category", "Property")]
public class CrossBorderTransferOptionsValidatorPropertyTests
{
    private readonly CrossBorderTransferOptionsValidator _validator = new();

    #region Null Options

    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => _validator.Validate(null, null!);

        Should.Throw<ArgumentNullException>(act);
    }

    #endregion

    #region DefaultSourceCountryCode Invariants

    /// <summary>
    /// Invariant: Empty or whitespace DefaultSourceCountryCode always causes validation failure.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property EmptySourceCountryCode_AlwaysFails()
    {
        return Prop.ForAll(
            Arb.From(Gen.Elements("", " ", "  ", "\t")),
            emptyCode =>
            {
                var options = CreateValidOptions();
                options.DefaultSourceCountryCode = emptyCode;

                var result = _validator.Validate(null, options);

                return result != ValidateOptionsResult.Success;
            });
    }

    /// <summary>
    /// Invariant: Any non-empty country code passes the source country code check.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property NonEmptySourceCountryCode_PassesCheck()
    {
        return Prop.ForAll(
            Arb.From(Gen.Elements("DE", "FR", "US", "JP", "BR", "AU")),
            code =>
            {
                var options = CreateValidOptions();
                options.DefaultSourceCountryCode = code;

                var result = _validator.Validate(null, options);

                return result == ValidateOptionsResult.Success;
            });
    }

    #endregion

    #region DefaultSCCExpirationDays Invariants

    /// <summary>
    /// Invariant: Zero or negative SCC expiration days always causes validation failure.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property InvalidSCCExpirationDays_AlwaysFails()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(-1000, 0)),
            expiration =>
            {
                var options = CreateValidOptions();
                options.DefaultSCCExpirationDays = expiration;

                var result = _validator.Validate(null, options);

                return result != ValidateOptionsResult.Success;
            });
    }

    /// <summary>
    /// Invariant: Positive SCC expiration days always pass validation.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property PositiveSCCExpirationDays_AlwaysPasses()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 3650)),
            expiration =>
            {
                var options = CreateValidOptions();
                options.DefaultSCCExpirationDays = expiration;

                var result = _validator.Validate(null, options);

                return result == ValidateOptionsResult.Success;
            });
    }

    #endregion

    #region DefaultTransferExpirationDays Invariants

    /// <summary>
    /// Invariant: Zero or negative transfer expiration days always causes validation failure.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property InvalidTransferExpirationDays_AlwaysFails()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(-1000, 0)),
            expiration =>
            {
                var options = CreateValidOptions();
                options.DefaultTransferExpirationDays = expiration;

                var result = _validator.Validate(null, options);

                return result != ValidateOptionsResult.Success;
            });
    }

    #endregion

    #region Multiple Invalid Fields

    /// <summary>
    /// Invariant: Multiple invalid fields still produce a failure result.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property MultipleInvalidFields_AlwaysFails()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(-100, -1)),
            Arb.From(Gen.Choose(-100, 0)),
            (negativeExpiration, negativeTtl) =>
            {
                var options = new CrossBorderTransferOptions
                {
                    TIARiskThreshold = 2.0,
                    CacheTTLMinutes = negativeTtl,
                    DefaultTIAExpirationDays = negativeExpiration,
                    DefaultSourceCountryCode = ""
                };

                var result = _validator.Validate(null, options);

                return result != ValidateOptionsResult.Success;
            });
    }

    #endregion

    #region Helpers

    private static CrossBorderTransferOptions CreateValidOptions()
    {
        return new CrossBorderTransferOptions
        {
            TIARiskThreshold = 0.6,
            CacheTTLMinutes = 5,
            DefaultTIAExpirationDays = 365,
            DefaultSCCExpirationDays = null,
            DefaultTransferExpirationDays = 365,
            DefaultSourceCountryCode = "DE"
        };
    }

    #endregion
}
