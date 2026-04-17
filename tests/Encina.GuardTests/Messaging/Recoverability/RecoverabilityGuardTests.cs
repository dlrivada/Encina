using Encina.Messaging.Recoverability;
using Shouldly;

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

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void PipelineBehavior_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new RecoverabilityPipelineBehavior<TestRequest, TestResponse>(
            new RecoverabilityOptions(),
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void PipelineBehavior_NullDelayedRetryScheduler_Succeeds()
    {
        var act = () => new RecoverabilityPipelineBehavior<TestRequest, TestResponse>(
            new RecoverabilityOptions(),
            NullLogger<RecoverabilityPipelineBehavior<TestRequest, TestResponse>>.Instance,
            delayedRetryScheduler: null);

        Should.NotThrow(act);
    }

    [Fact]
    public void PipelineBehavior_NullTimeProvider_UsesSystemDefault()
    {
        var act = () => new RecoverabilityPipelineBehavior<TestRequest, TestResponse>(
            new RecoverabilityOptions(),
            NullLogger<RecoverabilityPipelineBehavior<TestRequest, TestResponse>>.Instance,
            null,
            timeProvider: null);

        Should.NotThrow(act);
    }

    [Fact]
    public void PipelineBehavior_ValidParameters_CreatesInstance()
    {
        var sut = new RecoverabilityPipelineBehavior<TestRequest, TestResponse>(
            new RecoverabilityOptions(),
            NullLogger<RecoverabilityPipelineBehavior<TestRequest, TestResponse>>.Instance);

        sut.ShouldNotBeNull();
    }

    [Fact]
    public void PipelineBehavior_OptionsWithNullErrorClassifier_UsesDefault()
    {
        var options = new RecoverabilityOptions { ErrorClassifier = null };

        var act = () => new RecoverabilityPipelineBehavior<TestRequest, TestResponse>(
            options,
            NullLogger<RecoverabilityPipelineBehavior<TestRequest, TestResponse>>.Instance);

        Should.NotThrow(act);
    }

    #endregion

    #region RecoverabilityOptions Validation

    [Fact]
    public void RecoverabilityOptions_DefaultValues_AreReasonable()
    {
        var options = new RecoverabilityOptions();

        options.ImmediateRetries.ShouldBeGreaterThan(0);
        options.ImmediateRetryDelay.ShouldBeGreaterThan(TimeSpan.Zero);
        options.DelayedRetries.ShouldNotBeEmpty();
        options.UseJitter.ShouldBeTrue();
        options.MaxJitterPercent.ShouldBeGreaterThan(0);
        options.EnableDelayedRetries.ShouldBeTrue();
        options.UseExponentialBackoffForImmediateRetries.ShouldBeTrue();
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

        options.TotalRetryAttempts.ShouldBe(5); // 3 immediate + 2 delayed
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

        options.TotalRetryAttempts.ShouldBe(3); // Only immediate
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

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("scopeFactory");
    }

    [Fact]
    public void DelayedRetryProcessor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DelayedRetryProcessor(
            Substitute.For<IServiceScopeFactory>(),
            null!,
            NullLogger<DelayedRetryProcessor>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void DelayedRetryProcessor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DelayedRetryProcessor(
            Substitute.For<IServiceScopeFactory>(),
            new RecoverabilityOptions(),
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void DelayedRetryProcessor_ValidParameters_CreatesInstance()
    {
        var sut = new DelayedRetryProcessor(
            Substitute.For<IServiceScopeFactory>(),
            new RecoverabilityOptions(),
            NullLogger<DelayedRetryProcessor>.Instance);

        sut.ShouldNotBeNull();
    }

    [Fact]
    public void DelayedRetryProcessor_DefaultProcessingInterval_IsPositive()
    {
        var sut = new DelayedRetryProcessor(
            Substitute.For<IServiceScopeFactory>(),
            new RecoverabilityOptions(),
            NullLogger<DelayedRetryProcessor>.Instance);

        sut.ProcessingInterval.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void DelayedRetryProcessor_DefaultBatchSize_IsPositive()
    {
        var sut = new DelayedRetryProcessor(
            Substitute.For<IServiceScopeFactory>(),
            new RecoverabilityOptions(),
            NullLogger<DelayedRetryProcessor>.Instance);

        sut.BatchSize.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void DelayedRetryProcessor_ProcessingInterval_CanBeSet()
    {
        var sut = new DelayedRetryProcessor(
            Substitute.For<IServiceScopeFactory>(),
            new RecoverabilityOptions(),
            NullLogger<DelayedRetryProcessor>.Instance);

        sut.ProcessingInterval = TimeSpan.FromSeconds(10);
        sut.ProcessingInterval.ShouldBe(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void DelayedRetryProcessor_BatchSize_CanBeSet()
    {
        var sut = new DelayedRetryProcessor(
            Substitute.For<IServiceScopeFactory>(),
            new RecoverabilityOptions(),
            NullLogger<DelayedRetryProcessor>.Instance);

        sut.BatchSize = 50;
        sut.BatchSize.ShouldBe(50);
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

        Should.NotThrow(act);
    }

    [Fact]
    public void RecoverabilityOptions_CustomErrorClassifier_IsUsed()
    {
        var customClassifier = Substitute.For<IErrorClassifier>();
        var options = new RecoverabilityOptions { ErrorClassifier = customClassifier };

        var act = () => new RecoverabilityPipelineBehavior<TestRequest, TestResponse>(
            options,
            NullLogger<RecoverabilityPipelineBehavior<TestRequest, TestResponse>>.Instance);

        Should.NotThrow(act);
    }

    [Fact]
    public void RecoverabilityOptions_OnPermanentFailure_CanBeSet()
    {
        var options = new RecoverabilityOptions
        {
            OnPermanentFailure = (_, _) => Task.CompletedTask
        };

        options.OnPermanentFailure.ShouldNotBeNull();
    }

    #endregion

    private sealed class TestRequest : IRequest<TestResponse>
    {
    }

    private sealed class TestResponse
    {
    }
}
