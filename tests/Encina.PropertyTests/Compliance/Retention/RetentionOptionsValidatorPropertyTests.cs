using Encina.Compliance.Retention;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.Retention;

/// <summary>
/// Property-based tests for <see cref="RetentionOptionsValidator"/> verifying validation
/// invariants across randomized inputs using FsCheck.
/// </summary>
[Trait("Category", "Property")]
public class RetentionOptionsValidatorPropertyTests
{
    private readonly RetentionOptionsValidator _sut = new();

    /// <summary>
    /// Invariant: Any <see cref="RetentionOptions"/> with all valid values
    /// (defined enum, positive interval, non-negative alert days, positive or null default period)
    /// always produces a successful validation result.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ValidOptions_AlwaysSucceed()
    {
        var modeGen = Arb.From(Gen.Elements(
            RetentionEnforcementMode.Block,
            RetentionEnforcementMode.Warn,
            RetentionEnforcementMode.Disabled));

        var intervalGen = Arb.From(Gen.Choose(1, 1440).Select(m => TimeSpan.FromMinutes(m)));
        var alertDaysGen = Arb.From(Gen.Choose(0, 365));

        return Prop.ForAll(modeGen, intervalGen, alertDaysGen,
            (mode, interval, alertDays) =>
            {
                var options = new RetentionOptions
                {
                    EnforcementMode = mode,
                    EnforcementInterval = interval,
                    AlertBeforeExpirationDays = alertDays,
                    DefaultRetentionPeriod = TimeSpan.FromDays(365)
                };

                var result = _sut.Validate(null, options);

                return result.Succeeded.ToProperty();
            });
    }

    /// <summary>
    /// Invariant: An undefined <see cref="RetentionEnforcementMode"/> value always
    /// causes validation failure, regardless of other option values.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property InvalidEnforcementMode_AlwaysFails()
    {
        // Generate values outside the valid enum range (0-2)
        var invalidModeGen = Arb.From(Gen.Choose(3, 200).Select(v => (RetentionEnforcementMode)v));
        var intervalGen = Arb.From(Gen.Choose(1, 1440).Select(m => TimeSpan.FromMinutes(m)));

        return Prop.ForAll(invalidModeGen, intervalGen, (invalidMode, interval) =>
        {
            var options = new RetentionOptions
            {
                EnforcementMode = invalidMode,
                EnforcementInterval = interval,
                AlertBeforeExpirationDays = 30
            };

            var result = _sut.Validate(null, options);

            return result.Failed.ToProperty()
                .Label($"Expected failure for mode={(int)invalidMode}");
        });
    }

    /// <summary>
    /// Invariant: A non-positive enforcement interval always causes validation failure,
    /// regardless of other option values.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property NonPositiveInterval_AlwaysFails()
    {
        var negativeMinutesGen = Arb.From(Gen.Choose(-1440, 0).Select(m => TimeSpan.FromMinutes(m)));

        return Prop.ForAll(negativeMinutesGen, interval =>
        {
            var options = new RetentionOptions
            {
                EnforcementMode = RetentionEnforcementMode.Warn,
                EnforcementInterval = interval,
                AlertBeforeExpirationDays = 30
            };

            var result = _sut.Validate(null, options);

            return result.Failed.ToProperty()
                .Label($"Expected failure for interval={interval}");
        });
    }

    /// <summary>
    /// Invariant: Negative alert-before-expiration days always causes validation failure.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property NegativeAlertDays_AlwaysFails()
    {
        var negativeAlertGen = Arb.From(Gen.Choose(-365, -1));

        return Prop.ForAll(negativeAlertGen, alertDays =>
        {
            var options = new RetentionOptions
            {
                EnforcementMode = RetentionEnforcementMode.Warn,
                EnforcementInterval = TimeSpan.FromMinutes(60),
                AlertBeforeExpirationDays = alertDays
            };

            var result = _sut.Validate(null, options);

            return result.Failed.ToProperty()
                .Label($"Expected failure for alertDays={alertDays}");
        });
    }

    /// <summary>
    /// Invariant: A non-positive default retention period always causes validation failure.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property NonPositiveDefaultPeriod_AlwaysFails()
    {
        var nonPositivePeriodGen = Arb.From(Gen.Choose(-365, 0).Select(d => TimeSpan.FromDays(d)));

        return Prop.ForAll(nonPositivePeriodGen, period =>
        {
            var options = new RetentionOptions
            {
                EnforcementMode = RetentionEnforcementMode.Warn,
                EnforcementInterval = TimeSpan.FromMinutes(60),
                AlertBeforeExpirationDays = 30,
                DefaultRetentionPeriod = period
            };

            var result = _sut.Validate(null, options);

            return result.Failed.ToProperty()
                .Label($"Expected failure for defaultPeriod={period}");
        });
    }
}
