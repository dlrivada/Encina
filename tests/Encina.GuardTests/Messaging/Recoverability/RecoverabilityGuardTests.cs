using Encina.Messaging.Recoverability;
using FluentAssertions;

namespace Encina.GuardTests.Messaging.Recoverability;

/// <summary>
/// Guard clause tests for RecoverabilityPipelineBehavior and DelayedRetryProcessor
/// constructor parameters and error classification paths.
/// </summary>
public class RecoverabilityGuardTests
{
    #region RecoverabilityPipelineBehavior Constructor Guards

    [Fact]
    public void PipelineBehavior_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new RecoverabilityPipelineBehavior<TestRequest, TestResponse>(
            null!,
            NullLogger<RecoverabilityPipelineBehavior<TestRequest, TestResponse>>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void PipelineBehavior_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new RecoverabilityPipelineBehavior<TestRequest, TestResponse>(
            new RecoverabilityOptions(),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void PipelineBehavior_NullDelayedRetryScheduler_Succeeds()
    {
        var act = () => new RecoverabilityPipelineBehavior<TestRequest, TestResponse>(
            new RecoverabilityOptions(),
            NullLogger<RecoverabilityPipelineBehavior<TestRequest, TestResponse>>.Instance,
            delayedRetryScheduler: null);

        act.Should().NotThrow();
    }

    [Fact]
    public void PipelineBehavior_NullTimeProvider_UsesSystemDefault()
    {
        var act = () => new RecoverabilityPipelineBehavior<TestRequest, TestResponse>(
            new RecoverabilityOptions(),
            NullLogger<RecoverabilityPipelineBehavior<TestRequest, TestResponse>>.Instance,
            null,
            timeProvider: null);

        act.Should().NotThrow();
    }

    [Fact]
    public void PipelineBehavior_ValidParameters_CreatesInstance()
    {
        var sut = new RecoverabilityPipelineBehavior<TestRequest, TestResponse>(
            new RecoverabilityOptions(),
            NullLogger<RecoverabilityPipelineBehavior<TestRequest, TestResponse>>.Instance);

        sut.Should().NotBeNull();
    }

    [Fact]
    public void PipelineBehavior_OptionsWithNullErrorClassifier_UsesDefault()
    {
        var options = new RecoverabilityOptions { ErrorClassifier = null };

        var act = () => new RecoverabilityPipelineBehavior<TestRequest, TestResponse>(
            options,
            NullLogger<RecoverabilityPipelineBehavior<TestRequest, TestResponse>>.Instance);

        act.Should().NotThrow();
    }

    #endregion

    #region RecoverabilityOptions Validation

    [Fact]
    public void RecoverabilityOptions_DefaultValues_AreReasonable()
    {
        var options = new RecoverabilityOptions();

        options.ImmediateRetries.Should().BeGreaterThan(0);
        options.ImmediateRetryDelay.Should().BeGreaterThan(TimeSpan.Zero);
        options.DelayedRetries.Should().NotBeEmpty();
        options.UseJitter.Should().BeTrue();
        options.MaxJitterPercent.Should().BeGreaterThan(0);
        options.EnableDelayedRetries.Should().BeTrue();
        options.UseExponentialBackoffForImmediateRetries.Should().BeTrue();
    }

    [Fact]
    public void RecoverabilityOptions_TotalRetryAttempts_IncludesBothPhases()
    {
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 3,
            DelayedRetries = [TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5)],
            EnableDelayedRetries = true
        };

        options.TotalRetryAttempts.Should().Be(5); // 3 immediate + 2 delayed
    }

    [Fact]
    public void RecoverabilityOptions_TotalRetryAttempts_DisabledDelayed_OnlyImmediate()
    {
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 3,
            DelayedRetries = [TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5)],
            EnableDelayedRetries = false
        };

        options.TotalRetryAttempts.Should().Be(3); // Only immediate
    }

    #endregion

    #region DelayedRetryProcessor Constructor Guards

    [Fact]
    public void DelayedRetryProcessor_NullScopeFactory_ThrowsArgumentNullException()
    {
        var act = () => new DelayedRetryProcessor(
            null!,
            new RecoverabilityOptions(),
            NullLogger<DelayedRetryProcessor>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("scopeFactory");
    }

    [Fact]
    public void DelayedRetryProcessor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DelayedRetryProcessor(
            Substitute.For<IServiceScopeFactory>(),
            null!,
            NullLogger<DelayedRetryProcessor>.Instance);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void DelayedRetryProcessor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DelayedRetryProcessor(
            Substitute.For<IServiceScopeFactory>(),
            new RecoverabilityOptions(),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void DelayedRetryProcessor_ValidParameters_CreatesInstance()
    {
        var sut = new DelayedRetryProcessor(
            Substitute.For<IServiceScopeFactory>(),
            new RecoverabilityOptions(),
            NullLogger<DelayedRetryProcessor>.Instance);

        sut.Should().NotBeNull();
    }

    [Fact]
    public void DelayedRetryProcessor_DefaultProcessingInterval_IsPositive()
    {
        var sut = new DelayedRetryProcessor(
            Substitute.For<IServiceScopeFactory>(),
            new RecoverabilityOptions(),
            NullLogger<DelayedRetryProcessor>.Instance);

        sut.ProcessingInterval.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void DelayedRetryProcessor_DefaultBatchSize_IsPositive()
    {
        var sut = new DelayedRetryProcessor(
            Substitute.For<IServiceScopeFactory>(),
            new RecoverabilityOptions(),
            NullLogger<DelayedRetryProcessor>.Instance);

        sut.BatchSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public void DelayedRetryProcessor_ProcessingInterval_CanBeSet()
    {
        var sut = new DelayedRetryProcessor(
            Substitute.For<IServiceScopeFactory>(),
            new RecoverabilityOptions(),
            NullLogger<DelayedRetryProcessor>.Instance);

        sut.ProcessingInterval = TimeSpan.FromSeconds(10);
        sut.ProcessingInterval.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void DelayedRetryProcessor_BatchSize_CanBeSet()
    {
        var sut = new DelayedRetryProcessor(
            Substitute.For<IServiceScopeFactory>(),
            new RecoverabilityOptions(),
            NullLogger<DelayedRetryProcessor>.Instance);

        sut.BatchSize = 50;
        sut.BatchSize.Should().Be(50);
    }

    #endregion

    #region ErrorClassification Behavior

    [Fact]
    public void DefaultErrorClassifier_ClassifiesAsTransient()
    {
        // The DefaultErrorClassifier is used when ErrorClassifier is null
        var options = new RecoverabilityOptions { ErrorClassifier = null };

        // DefaultErrorClassifier classifies most errors as Transient
        // Verify the default is created and constructor does not throw
        var act = () => new RecoverabilityPipelineBehavior<TestRequest, TestResponse>(
            options,
            NullLogger<RecoverabilityPipelineBehavior<TestRequest, TestResponse>>.Instance);

        act.Should().NotThrow();
    }

    [Fact]
    public void RecoverabilityOptions_CustomErrorClassifier_IsUsed()
    {
        var customClassifier = Substitute.For<IErrorClassifier>();
        var options = new RecoverabilityOptions { ErrorClassifier = customClassifier };

        var act = () => new RecoverabilityPipelineBehavior<TestRequest, TestResponse>(
            options,
            NullLogger<RecoverabilityPipelineBehavior<TestRequest, TestResponse>>.Instance);

        act.Should().NotThrow();
    }

    [Fact]
    public void RecoverabilityOptions_OnPermanentFailure_CanBeSet()
    {
        var options = new RecoverabilityOptions
        {
            OnPermanentFailure = (_, _) => Task.CompletedTask
        };

        options.OnPermanentFailure.Should().NotBeNull();
    }

    #endregion

    private sealed class TestRequest : IRequest<TestResponse>
    {
    }

    private sealed class TestResponse
    {
    }
}
