using Encina.Security.BreakTheGlass;
using Shouldly;

namespace Encina.UnitTests.Security.BreakTheGlass;

/// <summary>Unit tests for <see cref="BreakTheGlassOptionsValidator"/> (internal; reached through InternalsVisibleTo).</summary>
[Trait("Category", "Unit")]
[Trait("Feature", "BreakTheGlass")]
public sealed class BreakTheGlassOptionsValidatorTests
{
    private static readonly BreakTheGlassOptionsValidator Validator = new();

    private static string[] Failures(BreakTheGlassOptions options) =>
        Validator.Validate(null, options).Failures?.ToArray() ?? [];

    [Fact]
    public void ValidOptions_Succeed()
    {
        Validator.Validate(null, BreakTheGlassTestKit.Options()).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void NullOptions_Throw() =>
        Should.Throw<ArgumentNullException>(() => Validator.Validate(null, null!));

    [Fact]
    public void NoScope_Fails()
    {
        var options = new BreakTheGlassOptions();
        options.ReviewerRoles.Add("reviewer");

        Failures(options).ShouldHaveSingleItem().ShouldContain("Scopes");
    }

    [Fact]
    public void ScopeWithoutOpener_Fails()
    {
        var options = BreakTheGlassTestKit.Options();
        options.DeclareScope("empty", _ => { });

        Failures(options).ShouldHaveSingleItem().ShouldContain("'empty' must name an opener");
    }

    [Fact]
    public void ScopeWithOnlyAPermission_Succeeds()
    {
        var options = BreakTheGlassTestKit.Options();
        options.DeclareScope("perm", scope => scope.OpenerPermissions.Add("records:break-glass"));

        Validator.Validate(null, options).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void NoReviewerRole_Fails()
    {
        var options = BreakTheGlassTestKit.Options();
        options.ReviewerRoles.Clear();

        Failures(options).ShouldHaveSingleItem().ShouldContain("ReviewerRoles");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(25)]
    public void MaxDurationOutOfRange_Fails(int hours)
    {
        var options = BreakTheGlassTestKit.Options(o => o.MaxDuration = TimeSpan.FromHours(hours));

        Failures(options).ShouldContain(f => f.Contains("MaxDuration"));
    }

    [Fact]
    public void MaxDurationAtTheLimit_Succeeds() =>
        Validator.Validate(null, BreakTheGlassTestKit.Options(o => o.MaxDuration = TimeSpan.FromHours(24))).Succeeded.ShouldBeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    public void SweepIntervalBelowTenSeconds_Fails(int seconds)
    {
        var options = BreakTheGlassTestKit.Options(o => o.ExpirySweepInterval = TimeSpan.FromSeconds(seconds));

        Failures(options).ShouldHaveSingleItem().ShouldContain("ExpirySweepInterval");
    }

    [Fact]
    public void TimerIntervalsAboveTheTimerLimit_Fail()
    {
        var options = BreakTheGlassTestKit.Options(o =>
        {
            o.ExpirySweepInterval = TimeSpan.FromDays(2);
            o.PurgeIntervalHours = 721;
        });

        var failures = Failures(options);

        failures.Length.ShouldBe(2);
        failures.ShouldContain(f => f.Contains("ExpirySweepInterval"));
        failures.ShouldContain(f => f.Contains("PurgeIntervalHours"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public void BatchSizeOutOfRange_Fails(int size)
    {
        var options = BreakTheGlassTestKit.Options(o => o.ExpirySweepBatchSize = size);

        Failures(options).ShouldHaveSingleItem().ShouldContain("ExpirySweepBatchSize");
    }

    [Fact]
    public void NonPositiveRetentionAndPurgeInterval_Fail()
    {
        var options = BreakTheGlassTestKit.Options(o =>
        {
            o.RetentionDays = 0;
            o.PurgeIntervalHours = 0;
        });

        var failures = Failures(options);

        failures.Length.ShouldBe(2);
        failures.ShouldContain(f => f.Contains("RetentionDays"));
        failures.ShouldContain(f => f.Contains("PurgeIntervalHours"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2001)]
    public void MinimumJustificationLengthOutOfRange_Fails(int length)
    {
        var options = BreakTheGlassTestKit.Options(o => o.MinimumJustificationLength = length);

        Failures(options).ShouldHaveSingleItem().ShouldContain("MinimumJustificationLength");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void ScopeMaxDurationNotWithinTheGlobalLimit_Fails(int hours)
    {
        var options = BreakTheGlassTestKit.Options();
        options.DeclareScope("tight", scope =>
        {
            scope.OpenerRoles.Add("clinician");
            scope.MaxDuration = TimeSpan.FromHours(hours);
        });

        Failures(options).ShouldHaveSingleItem().ShouldContain("'tight' MaxDuration");
    }

    [Fact]
    public void BlankScopeName_Fails()
    {
        var options = BreakTheGlassTestKit.Options();
        options.Scopes[" "] = new BreakTheGlassScopeOptions();
        options.Scopes[" "].OpenerRoles.Add("clinician");

        Failures(options).ShouldHaveSingleItem().ShouldContain("must not be blank");
    }

    [Fact]
    public void EveryFailureIsReportedAtOnce()
    {
        var options = new BreakTheGlassOptions { MaxDuration = TimeSpan.Zero, ExpirySweepBatchSize = 0 };

        Failures(options).Length.ShouldBe(4);
    }

    [Fact]
    public void Disabled_SkipsTheFeatureRules_ButKeepsTheSafetyBounds()
    {
        var empty = new BreakTheGlassOptions { Enabled = false };
        Validator.Validate(null, empty).Succeeded.ShouldBeTrue();

        var unsafeBounds = new BreakTheGlassOptions { Enabled = false, MaxDuration = TimeSpan.FromDays(2), ExpirySweepInterval = TimeSpan.FromSeconds(1) };
        Failures(unsafeBounds).Length.ShouldBe(2);
    }
}
