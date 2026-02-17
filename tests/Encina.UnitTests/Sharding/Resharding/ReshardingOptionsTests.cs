using Encina.Sharding.Resharding;
using Shouldly;

namespace Encina.UnitTests.Sharding.Resharding;

/// <summary>
/// Unit tests for <see cref="ReshardingOptions"/>.
/// Verifies default values, property setters, and callback handling.
/// </summary>
public sealed class ReshardingOptionsTests
{
    #region Default Values

    [Fact]
    public void Defaults_CopyBatchSize_Is10000()
    {
        var options = new ReshardingOptions();

        options.CopyBatchSize.ShouldBe(10_000);
    }

    [Fact]
    public void Defaults_CdcLagThreshold_Is5Seconds()
    {
        var options = new ReshardingOptions();

        options.CdcLagThreshold.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Defaults_VerificationMode_IsCountAndChecksum()
    {
        var options = new ReshardingOptions();

        options.VerificationMode.ShouldBe(VerificationMode.CountAndChecksum);
    }

    [Fact]
    public void Defaults_CutoverTimeout_Is30Seconds()
    {
        var options = new ReshardingOptions();

        options.CutoverTimeout.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Defaults_CleanupRetentionPeriod_Is24Hours()
    {
        var options = new ReshardingOptions();

        options.CleanupRetentionPeriod.ShouldBe(TimeSpan.FromHours(24));
    }

    [Fact]
    public void Defaults_OnPhaseCompleted_IsNull()
    {
        var options = new ReshardingOptions();

        options.OnPhaseCompleted.ShouldBeNull();
    }

    [Fact]
    public void Defaults_OnCutoverStarting_IsNull()
    {
        var options = new ReshardingOptions();

        options.OnCutoverStarting.ShouldBeNull();
    }

    #endregion

    #region Property Setters

    [Fact]
    public void CopyBatchSize_SetAndGet_ReturnsSetValue()
    {
        var options = new ReshardingOptions { CopyBatchSize = 50_000 };

        options.CopyBatchSize.ShouldBe(50_000);
    }

    [Fact]
    public void CdcLagThreshold_SetAndGet_ReturnsSetValue()
    {
        var options = new ReshardingOptions { CdcLagThreshold = TimeSpan.FromSeconds(10) };

        options.CdcLagThreshold.ShouldBe(TimeSpan.FromSeconds(10));
    }

    [Theory]
    [InlineData(VerificationMode.Count)]
    [InlineData(VerificationMode.Checksum)]
    [InlineData(VerificationMode.CountAndChecksum)]
    public void VerificationMode_SetAndGet_ReturnsSetValue(VerificationMode mode)
    {
        var options = new ReshardingOptions { VerificationMode = mode };

        options.VerificationMode.ShouldBe(mode);
    }

    [Fact]
    public void CutoverTimeout_SetAndGet_ReturnsSetValue()
    {
        var options = new ReshardingOptions { CutoverTimeout = TimeSpan.FromMinutes(5) };

        options.CutoverTimeout.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void CleanupRetentionPeriod_SetAndGet_ReturnsSetValue()
    {
        var options = new ReshardingOptions { CleanupRetentionPeriod = TimeSpan.FromHours(48) };

        options.CleanupRetentionPeriod.ShouldBe(TimeSpan.FromHours(48));
    }

    [Fact]
    public void OnPhaseCompleted_SetAndGet_ReturnsSetCallback()
    {
        Func<ReshardingPhase, ReshardingProgress, Task> callback = (_, _) => Task.CompletedTask;
        var options = new ReshardingOptions { OnPhaseCompleted = callback };

        options.OnPhaseCompleted.ShouldBe(callback);
    }

    [Fact]
    public void OnCutoverStarting_SetAndGet_ReturnsSetPredicate()
    {
        Func<ReshardingPlan, CancellationToken, Task<bool>> predicate = (_, _) => Task.FromResult(true);
        var options = new ReshardingOptions { OnCutoverStarting = predicate };

        options.OnCutoverStarting.ShouldBe(predicate);
    }

    #endregion
}
