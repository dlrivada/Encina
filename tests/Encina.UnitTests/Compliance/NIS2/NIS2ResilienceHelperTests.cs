#pragma warning disable CA2012 // Use ValueTasks correctly (test assertions)

using Encina.Compliance.NIS2;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Polly;
using Polly.Registry;

namespace Encina.UnitTests.Compliance.NIS2;

/// <summary>
/// Unit tests for <see cref="NIS2ResilienceHelper"/>, the internal resilience infrastructure
/// that wraps all NIS2 external calls with pipeline-or-timeout protection.
/// </summary>
public class NIS2ResilienceHelperTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    #region ExecuteAsync<T> — Pipeline Path

    [Fact]
    public async Task ExecuteAsync_WithRegisteredPipeline_ShouldUseIt()
    {
        // Arrange — register a passthrough pipeline
        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder(NIS2ResilienceHelper.PipelineKey,
            (builder, _) => builder.AddTimeout(TimeSpan.FromSeconds(10)));
        var sp = BuildServiceProvider(registry);

        // Act
        var result = await NIS2ResilienceHelper.ExecuteAsync(
            sp,
            _ => new ValueTask<int>(42),
            fallback: -1,
            DefaultTimeout,
            CancellationToken.None);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteAsync_PipelineKeyNotRegistered_ShouldFallThroughToTimeout()
    {
        // Arrange — provider exists but pipeline key "nis2-external" is NOT registered
        var registry = new ResiliencePipelineRegistry<string>();
        // Register a DIFFERENT key
        registry.TryAddBuilder("other-pipeline",
            (builder, _) => builder.AddTimeout(TimeSpan.FromSeconds(10)));
        var sp = BuildServiceProvider(registry);

        // Act — should fall through KeyNotFoundException to timeout path
        var result = await NIS2ResilienceHelper.ExecuteAsync(
            sp,
            _ => new ValueTask<string>("from-timeout-path"),
            fallback: "fallback",
            DefaultTimeout,
            CancellationToken.None);

        // Assert — succeeded via timeout path
        result.Should().Be("from-timeout-path");
    }

    [Fact]
    public async Task ExecuteAsync_PipelineExecutionFails_ShouldReturnFallback()
    {
        // Arrange — register a pipeline that will execute, but the operation throws
        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder(NIS2ResilienceHelper.PipelineKey,
            (builder, _) => builder.AddTimeout(TimeSpan.FromSeconds(10)));
        var sp = BuildServiceProvider(registry);

        // Act — operation fails inside the pipeline
        var result = await NIS2ResilienceHelper.ExecuteAsync(
            sp,
            _ => throw new InvalidOperationException("external service down"),
            fallback: "safe-value",
            DefaultTimeout,
            CancellationToken.None);

        // Assert
        result.Should().Be("safe-value");
    }

    #endregion

    #region ExecuteAsync<T> — Timeout Fallback Path

    [Fact]
    public async Task ExecuteAsync_NoPipelineProvider_ShouldSucceedViaTimeoutPath()
    {
        // Arrange — no ResiliencePipelineProvider registered
        var sp = new ServiceCollection().BuildServiceProvider();

        // Act
        var result = await NIS2ResilienceHelper.ExecuteAsync(
            sp,
            _ => new ValueTask<int>(99),
            fallback: -1,
            DefaultTimeout,
            CancellationToken.None);

        // Assert
        result.Should().Be(99);
    }

    [Fact]
    public async Task ExecuteAsync_TimeoutPathFails_ShouldReturnFallback()
    {
        // Arrange — no pipeline, operation throws
        var sp = new ServiceCollection().BuildServiceProvider();

        // Act
        var result = await NIS2ResilienceHelper.ExecuteAsync(
            sp,
            _ => throw new TimeoutException("timed out"),
            fallback: "fallback-value",
            DefaultTimeout,
            CancellationToken.None);

        // Assert
        result.Should().Be("fallback-value");
    }

    [Fact]
    public async Task ExecuteAsync_TimeoutPathCancelled_ShouldReturnFallback()
    {
        // Arrange — very short timeout to force cancellation
        var sp = new ServiceCollection().BuildServiceProvider();

        // Act — operation takes too long
        var result = await NIS2ResilienceHelper.ExecuteAsync(
            sp,
            async ct =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                return "never-reached";
            },
            fallback: "timed-out",
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Assert
        result.Should().Be("timed-out");
    }

    #endregion

    #region ExecuteAsync (void overload) — Pipeline Path

    [Fact]
    public async Task ExecuteAsyncVoid_WithRegisteredPipeline_ShouldExecuteOperation()
    {
        // Arrange
        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder(NIS2ResilienceHelper.PipelineKey,
            (builder, _) => builder.AddTimeout(TimeSpan.FromSeconds(10)));
        var sp = BuildServiceProvider(registry);
        var executed = false;

        // Act
        await NIS2ResilienceHelper.ExecuteAsync(
            sp,
            _ => { executed = true; return ValueTask.CompletedTask; },
            DefaultTimeout,
            CancellationToken.None);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsyncVoid_PipelineExecutionFails_ShouldSwallowSilently()
    {
        // Arrange
        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder(NIS2ResilienceHelper.PipelineKey,
            (builder, _) => builder.AddTimeout(TimeSpan.FromSeconds(10)));
        var sp = BuildServiceProvider(registry);

        // Act — should NOT throw
        var act = () => NIS2ResilienceHelper.ExecuteAsync(
            sp,
            _ => throw new InvalidOperationException("boom"),
            DefaultTimeout,
            CancellationToken.None).AsTask();

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region ExecuteAsync (void overload) — Timeout Fallback Path

    [Fact]
    public async Task ExecuteAsyncVoid_NoPipeline_ShouldExecuteViaTimeout()
    {
        // Arrange
        var sp = new ServiceCollection().BuildServiceProvider();
        var executed = false;

        // Act
        await NIS2ResilienceHelper.ExecuteAsync(
            sp,
            _ => { executed = true; return ValueTask.CompletedTask; },
            DefaultTimeout,
            CancellationToken.None);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsyncVoid_TimeoutPathFails_ShouldSwallowSilently()
    {
        // Arrange
        var sp = new ServiceCollection().BuildServiceProvider();

        // Act — should NOT throw
        var act = () => NIS2ResilienceHelper.ExecuteAsync(
            sp,
            _ => throw new IOException("network error"),
            DefaultTimeout,
            CancellationToken.None).AsTask();

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region PipelineKey constant

    [Fact]
    public void PipelineKey_ShouldBeNis2External()
    {
        NIS2ResilienceHelper.PipelineKey.Should().Be("nis2-external");
    }

    #endregion

    #region Helpers

    private static ServiceProvider BuildServiceProvider(ResiliencePipelineRegistry<string> registry)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ResiliencePipelineProvider<string>>(registry);
        return services.BuildServiceProvider();
    }

    #endregion
}
