using BenchmarkDotNet.Attributes;
using Encina.OpenTelemetry;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace Encina.Benchmarks;

/// <summary>
/// Benchmarks for Encina.OpenTelemetry instrumentation.
/// Compares overhead of OpenTelemetry tracing.
/// </summary>
[MemoryDiagnoser]
public class OpenTelemetryBenchmarks
{
    private IServiceProvider _providerWithoutOtel = default!;
    private IServiceProvider _providerWithOtel = default!;

    [GlobalSetup]
    public void Setup()
    {
        // Provider WITHOUT OpenTelemetry (baseline)
        var servicesNoOtel = new ServiceCollection();
        servicesNoOtel.AddEncina(config => { });
        servicesNoOtel.AddScoped<IRequestHandler<BenchmarkRequest, string>, BenchmarkRequestHandler>();
        servicesNoOtel.AddScoped<INotificationHandler<BenchmarkNotification>, BenchmarkNotificationHandler>();
        _providerWithoutOtel = servicesNoOtel.BuildServiceProvider();

        // Provider WITH OpenTelemetry instrumentation
        var servicesWithOtel = new ServiceCollection();
        servicesWithOtel.AddEncina(config => { });
        servicesWithOtel.AddScoped<IRequestHandler<BenchmarkRequest, string>, BenchmarkRequestHandler>();
        servicesWithOtel.AddScoped<INotificationHandler<BenchmarkNotification>, BenchmarkNotificationHandler>();

        servicesWithOtel.AddOpenTelemetry()
            .WithTracing(builder => builder
                .AddEncinaInstrumentation()
                .AddConsoleExporter());

        _providerWithOtel = servicesWithOtel.BuildServiceProvider();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        (_providerWithoutOtel as IDisposable)?.Dispose();
        (_providerWithOtel as IDisposable)?.Dispose();
    }

    /// <summary>
    /// Baseline: Send request WITHOUT OpenTelemetry.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<string> Send_Request_Baseline_WithoutOpenTelemetry()
    {
        using var scope = _providerWithoutOtel.CreateScope();
        var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var result = await Encina.Send(new BenchmarkRequest { Value = 42 }, CancellationToken.None);

        return result.Match(
            Right: value => value,
            Left: error => throw new InvalidOperationException(error.Message)
        );
    }

    /// <summary>
    /// Send request WITH OpenTelemetry instrumentation.
    /// </summary>
    [Benchmark]
    public async Task<string> Send_Request_WithOpenTelemetry()
    {
        using var scope = _providerWithOtel.CreateScope();
        var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var result = await Encina.Send(new BenchmarkRequest { Value = 42 }, CancellationToken.None);

        return result.Match(
            Right: value => value,
            Left: error => throw new InvalidOperationException(error.Message)
        );
    }

    /// <summary>
    /// Baseline: Publish notification WITHOUT OpenTelemetry.
    /// </summary>
    [Benchmark]
    public async Task Publish_Notification_Baseline_WithoutOpenTelemetry()
    {
        using var scope = _providerWithoutOtel.CreateScope();
        var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var result = await Encina.Publish(new BenchmarkNotification { Message = "test" }, CancellationToken.None);

        result.Match(
            Right: _ => { },
            Left: error => throw new InvalidOperationException(error.Message)
        );
    }

    /// <summary>
    /// Publish notification WITH OpenTelemetry instrumentation.
    /// </summary>
    [Benchmark]
    public async Task Publish_Notification_WithOpenTelemetry()
    {
        using var scope = _providerWithOtel.CreateScope();
        var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var result = await Encina.Publish(new BenchmarkNotification { Message = "test" }, CancellationToken.None);

        result.Match(
            Right: _ => { },
            Left: error => throw new InvalidOperationException(error.Message)
        );
    }

    #region Test Helpers

    private sealed record BenchmarkRequest : IRequest<string>
    {
        public int Value { get; init; }
    }

    private sealed class BenchmarkRequestHandler : IRequestHandler<BenchmarkRequest, string>
    {
        public Task<Either<EncinaError, string>> Handle(BenchmarkRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult<Either<EncinaError, string>>($"processed-{request.Value}");
        }
    }

    private sealed record BenchmarkNotification : INotification
    {
        public string Message { get; init; } = string.Empty;
    }

    private sealed class BenchmarkNotificationHandler : INotificationHandler<BenchmarkNotification>
    {
        public Task<Either<EncinaError, Unit>> Handle(BenchmarkNotification notification, CancellationToken cancellationToken)
        {
            return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
        }
    }

    #endregion
}
