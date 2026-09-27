#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.Health;
using Encina.Compliance.DPIA.Model;
using Encina.Compliance.DPIA.ReadModels;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Regression tests for <see cref="DPIAHealthCheck"/> ensuring that neither
/// <see cref="EncinaError.Message"/> nor a caught <see cref="Exception.Message"/> reaches the
/// <see cref="HealthCheckResult"/> description or data (AGENTS.md: "EncinaError.Message NEVER
/// reaches logs, activity tags, health-check results or plaintext storage"). See #1499.
/// </summary>
public class DPIAHealthCheckMessageLeakTests
{
    private const string SentinelMessage = "SENTINEL-do-not-leak-8f21c9b4";

    [Fact]
    public async Task CheckHealthAsync_ExpiredAssessmentsQueryFails_DoesNotLeakErrorMessage()
    {
        var service = Substitute.For<IDPIAService>();
        service.GetExpiredAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Prelude.Left<EncinaError, IReadOnlyList<DPIAReadModel>>(
                    EncinaErrors.Create("dpia.query-failed", SentinelMessage))));

        service.GetAllAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Prelude.Right<EncinaError, IReadOnlyList<DPIAReadModel>>(
                    Array.Empty<DPIAReadModel>() as IReadOnlyList<DPIAReadModel>)));

        var sut = CreateHealthCheck(new DPIAOptions(), service);

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Description.ShouldNotBeNull();
        result.Description.ShouldNotContain(SentinelMessage);

        foreach (var value in result.Data.Values)
        {
            ValueDoesNotContainSentinel(value);
        }

        result.Exception.ShouldBeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_ExpiredAssessmentsQueryThrows_DoesNotLeakExceptionMessage()
    {
        var service = Substitute.For<IDPIAService>();
        service.GetExpiredAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, IReadOnlyList<DPIAReadModel>>>>(
                _ => throw new InvalidOperationException(SentinelMessage));

        service.GetAllAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Prelude.Right<EncinaError, IReadOnlyList<DPIAReadModel>>(
                    Array.Empty<DPIAReadModel>() as IReadOnlyList<DPIAReadModel>)));

        var sut = CreateHealthCheck(new DPIAOptions(), service);

        var result = await sut.CheckHealthAsync(CreateContext());

        result.Description.ShouldNotBeNull();
        result.Description.ShouldNotContain(SentinelMessage);

        foreach (var value in result.Data.Values)
        {
            ValueDoesNotContainSentinel(value);
        }

        result.Exception.ShouldBeNull();
    }

    private static void ValueDoesNotContainSentinel(object? value)
    {
        switch (value)
        {
            case string text:
                text.ShouldNotContain(SentinelMessage);
                break;
            case IEnumerable<string> texts:
                foreach (var text in texts)
                {
                    text.ShouldNotContain(SentinelMessage);
                }

                break;
        }
    }

    private static DPIAHealthCheck CreateHealthCheck(
        DPIAOptions options,
        IDPIAService? service = null,
        IDPIAAssessmentEngine? engine = null)
    {
        service ??= Substitute.For<IDPIAService>();
        engine ??= Substitute.For<IDPIAAssessmentEngine>();

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(service);
        services.AddSingleton(engine);
        services.AddSingleton(TimeProvider.System);

        var provider = services.BuildServiceProvider();

        return new DPIAHealthCheck(provider, new NullLogger<DPIAHealthCheck>());
    }

    private static HealthCheckContext CreateContext() => new()
    {
        Registration = new HealthCheckRegistration(
            DPIAHealthCheck.DefaultName,
            Substitute.For<IHealthCheck>(),
            null,
            null)
    };
}
