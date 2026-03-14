using Encina.Compliance.CrossBorderTransfer;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

using Microsoft.Extensions.Options;

namespace Encina.PropertyTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Property-based tests for <see cref="CrossBorderTransferOptions"/> and
/// <see cref="CrossBorderTransferOptionsValidator"/> verifying validation
/// invariants across randomized inputs using FsCheck.
/// </summary>
[Trait("Category", "Property")]
public class CrossBorderTransferOptionsPropertyTests
{
    private readonly CrossBorderTransferOptionsValidator _validator = new();

    #region Valid Configuration Invariants

    /// <summary>
    /// Invariant: Options with a risk threshold in [0.0, 1.0], positive cache TTL,
    /// and positive expiration days always pass validation.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property ValidOptions_AlwaysPass()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(0, 100)),
            Arb.From(Gen.Choose(1, 1000)),
            Arb.From(Gen.Choose(1, 3650)),
            (thresholdInt, ttl, expiration) =>
            {
                var threshold = thresholdInt / 100.0;
                var options = new CrossBorderTransferOptions
                {
                    TIARiskThreshold = threshold,
                    CacheTTLMinutes = ttl,
                    DefaultTIAExpirationDays = expiration,
                    DefaultSCCExpirationDays = expiration,
                    DefaultTransferExpirationDays = expiration,
                    DefaultSourceCountryCode = "DE"
                };

                var result = _validator.Validate(null, options);

                return result == ValidateOptionsResult.Success;
            });
    }

    #endregion

    #region Risk Threshold Invariants

    /// <summary>
    /// Invariant: Any risk threshold above 1.0 causes validation failure.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property InvalidRiskThreshold_AboveOne_AlwaysFails()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(101, 200)),
            thresholdInt =>
            {
                var threshold = thresholdInt / 100.0;
                var options = CreateValidOptions();
                options.TIARiskThreshold = threshold;

                var result = _validator.Validate(null, options);

                return result != ValidateOptionsResult.Success;
            });
    }

    /// <summary>
    /// Invariant: Any risk threshold below 0.0 causes validation failure.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property InvalidRiskThreshold_BelowZero_AlwaysFails()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 200)),
            positiveInt =>
            {
                var threshold = -positiveInt / 100.0;
                var options = CreateValidOptions();
                options.TIARiskThreshold = threshold;

                var result = _validator.Validate(null, options);

                return result != ValidateOptionsResult.Success;
            });
    }

    #endregion

    #region Cache TTL Invariants

    /// <summary>
    /// Invariant: Any cache TTL of zero or negative causes validation failure.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property InvalidCacheTTL_AlwaysFails()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(-1000, 0)),
            ttl =>
            {
                var options = CreateValidOptions();
                options.CacheTTLMinutes = ttl;

                var result = _validator.Validate(null, options);

                return result != ValidateOptionsResult.Success;
            });
    }

    #endregion

    #region Expiration Invariants

    /// <summary>
    /// Invariant: When expiration days are null, the validator passes regardless
    /// of other valid settings (null means no auto-expiration).
    /// </summary>
    [Property(MaxTest = 50)]
    public Property NullExpiration_AlwaysPass()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(0, 100)),
            Arb.From(Gen.Choose(1, 1000)),
            (thresholdInt, ttl) =>
            {
                var threshold = thresholdInt / 100.0;
                var options = new CrossBorderTransferOptions
                {
                    TIARiskThreshold = threshold,
                    CacheTTLMinutes = ttl,
                    DefaultTIAExpirationDays = null,
                    DefaultSCCExpirationDays = null,
                    DefaultTransferExpirationDays = null,
                    DefaultSourceCountryCode = "DE"
                };

                var result = _validator.Validate(null, options);

                return result == ValidateOptionsResult.Success;
            });
    }

    /// <summary>
    /// Invariant: Negative expiration days always cause validation failure.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property NegativeExpiration_AlwaysFails()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(-1000, -1)),
            expiration =>
            {
                var options = CreateValidOptions();
                options.DefaultTIAExpirationDays = expiration;

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
