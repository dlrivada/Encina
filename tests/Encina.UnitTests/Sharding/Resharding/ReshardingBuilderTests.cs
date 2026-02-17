using Encina.Sharding.Resharding;
using Shouldly;

namespace Encina.UnitTests.Sharding.Resharding;

/// <summary>
/// Unit tests for <see cref="ReshardingBuilder"/>.
/// Verifies fluent API, default values, guard clauses, and Build() behavior.
/// </summary>
public sealed class ReshardingBuilderTests
{
    #region Default Values

    [Fact]
    public void Defaults_CopyBatchSize_Is10000()
    {
        var builder = new ReshardingBuilder();

        builder.CopyBatchSize.ShouldBe(10_000);
    }

    [Fact]
    public void Defaults_CdcLagThreshold_Is5Seconds()
    {
        var builder = new ReshardingBuilder();

        builder.CdcLagThreshold.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Defaults_VerificationMode_IsCountAndChecksum()
    {
        var builder = new ReshardingBuilder();

        builder.VerificationMode.ShouldBe(VerificationMode.CountAndChecksum);
    }

    [Fact]
    public void Defaults_CutoverTimeout_Is30Seconds()
    {
        var builder = new ReshardingBuilder();

        builder.CutoverTimeout.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Defaults_CleanupRetentionPeriod_Is24Hours()
    {
        var builder = new ReshardingBuilder();

        builder.CleanupRetentionPeriod.ShouldBe(TimeSpan.FromHours(24));
    }

    #endregion

    #region Property Setters

    [Fact]
    public void CopyBatchSize_SetAndGet_ReturnsSetValue()
    {
        var builder = new ReshardingBuilder { CopyBatchSize = 50_000 };

        builder.CopyBatchSize.ShouldBe(50_000);
    }

    [Fact]
    public void CdcLagThreshold_SetAndGet_ReturnsSetValue()
    {
        var builder = new ReshardingBuilder { CdcLagThreshold = TimeSpan.FromSeconds(10) };

        builder.CdcLagThreshold.ShouldBe(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void CutoverTimeout_SetAndGet_ReturnsSetValue()
    {
        var builder = new ReshardingBuilder { CutoverTimeout = TimeSpan.FromMinutes(2) };

        builder.CutoverTimeout.ShouldBe(TimeSpan.FromMinutes(2));
    }

    #endregion

    #region OnPhaseCompleted

    [Fact]
    public void OnPhaseCompleted_NullCallback_ThrowsArgumentNullException()
    {
        var builder = new ReshardingBuilder();

        Should.Throw<ArgumentNullException>(() =>
            builder.OnPhaseCompleted(null!));
    }

    [Fact]
    public void OnPhaseCompleted_ValidCallback_ReturnsBuilder()
    {
        var builder = new ReshardingBuilder();

        var result = builder.OnPhaseCompleted((_, _) => Task.CompletedTask);

        result.ShouldBeSameAs(builder);
    }

    #endregion

    #region OnCutoverStarting

    [Fact]
    public void OnCutoverStarting_NullPredicate_ThrowsArgumentNullException()
    {
        var builder = new ReshardingBuilder();

        Should.Throw<ArgumentNullException>(() =>
            builder.OnCutoverStarting(null!));
    }

    [Fact]
    public void OnCutoverStarting_ValidPredicate_ReturnsBuilder()
    {
        var builder = new ReshardingBuilder();

        var result = builder.OnCutoverStarting((_, _) => Task.FromResult(true));

        result.ShouldBeSameAs(builder);
    }

    #endregion

    #region Build

    [Fact]
    public void Build_DefaultBuilder_ReturnsOptionsWithDefaults()
    {
        var builder = new ReshardingBuilder();

        var options = builder.Build();

        options.ShouldNotBeNull();
        options.CopyBatchSize.ShouldBe(10_000);
        options.CdcLagThreshold.ShouldBe(TimeSpan.FromSeconds(5));
        options.VerificationMode.ShouldBe(VerificationMode.CountAndChecksum);
    }

    [Fact]
    public void Build_ConfiguredBuilder_ReturnsConfiguredOptions()
    {
        var builder = new ReshardingBuilder
        {
            CopyBatchSize = 25_000,
            CdcLagThreshold = TimeSpan.FromSeconds(3),
            VerificationMode = VerificationMode.Checksum,
            CutoverTimeout = TimeSpan.FromMinutes(1),
            CleanupRetentionPeriod = TimeSpan.FromHours(12)
        };

        var options = builder.Build();

        options.CopyBatchSize.ShouldBe(25_000);
        options.CdcLagThreshold.ShouldBe(TimeSpan.FromSeconds(3));
        options.VerificationMode.ShouldBe(VerificationMode.Checksum);
        options.CutoverTimeout.ShouldBe(TimeSpan.FromMinutes(1));
        options.CleanupRetentionPeriod.ShouldBe(TimeSpan.FromHours(12));
    }

    [Fact]
    public void Build_WithCallbacks_ReturnsOptionsWithCallbacks()
    {
        Func<ReshardingPhase, ReshardingProgress, Task> phaseCallback = (_, _) => Task.CompletedTask;
        Func<ReshardingPlan, CancellationToken, Task<bool>> cutoverPredicate = (_, _) => Task.FromResult(true);

        var builder = new ReshardingBuilder();
        builder.OnPhaseCompleted(phaseCallback);
        builder.OnCutoverStarting(cutoverPredicate);

        var options = builder.Build();

        options.OnPhaseCompleted.ShouldBe(phaseCallback);
        options.OnCutoverStarting.ShouldBe(cutoverPredicate);
    }

    #endregion
}
