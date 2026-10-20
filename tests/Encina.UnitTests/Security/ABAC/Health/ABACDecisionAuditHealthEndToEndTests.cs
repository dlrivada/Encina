#pragma warning disable CA2012 // Use ValueTasks correctly — NSubstitute mock setup pattern

using Encina.Security.ABAC;
using Encina.Security.ABAC.Health;
using Encina.Security.Audit;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.ABAC.Health;

/// <summary>
/// End to end through a real container (#751 Phase 5): a request sent through <see cref="IEncina"/> reaches the
/// ABAC Policy Enforcement Point, the operation audit store fails the write, and the registered health check
/// reports the failure.
/// </summary>
public sealed class ABACDecisionAuditHealthEndToEndTests
{
    [RequirePolicy("any-policy")]
    private sealed record GuardedRequest : IRequest<string>;

    private sealed class GuardedHandler : IRequestHandler<GuardedRequest, string>
    {
        public Task<Either<EncinaError, string>> Handle(GuardedRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(Right<EncinaError, string>("handled"));
    }

    private static ServiceProvider Build(IOperationAuditStore store)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina();
        services.AddScoped<IRequestHandler<GuardedRequest, string>, GuardedHandler>();
        services.AddScoped(_ => store);
        services.AddEncinaABAC(options =>
        {
            options.AuditDecisions();
            options.AddHealthCheck = true;
        });

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static IOperationAuditStore FailingStore()
    {
        var store = Substitute.For<IOperationAuditStore>();
        store.RecordAsync(Arg.Any<OperationAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, Unit>(EncinaErrors.Create("store.down", "x"))));
        store.GetByCorrelationIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, IReadOnlyList<OperationAuditEntry>>(EncinaErrors.Create("store.down", "x"))));
        return store;
    }

    private static async Task<HealthReportEntry> CheckAsync(ServiceProvider provider)
    {
        var report = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync();
        return report.Entries[ABACHealthCheck.DefaultName];
    }

    [Fact]
    public async Task AFailedAuditWrite_SeenByThePep_MakesTheRegisteredHealthCheckUnhealthy()
    {
        await using var provider = Build(FailingStore());
        await using var scope = provider.CreateAsyncScope();

        var result = await scope.ServiceProvider.GetRequiredService<IEncina>().Send(new GuardedRequest());

        // The caller carries no identity, so the PEP denies and records it; the store cannot write the record.
        result.IsLeft.ShouldBeTrue();
        var entry = await CheckAsync(provider);
        entry.Status.ShouldBe(HealthStatus.Unhealthy);
        entry.Data["decision_audit"].ShouldBe("write_failed");
    }

    [Fact]
    public async Task ASuccessfulAuditWrite_LeavesTheRegisteredHealthCheckFreeOfAWriteFailure()
    {
        var store = Substitute.For<IOperationAuditStore>();
        store.RecordAsync(Arg.Any<OperationAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default)));
        await using var provider = Build(store);
        await using var scope = provider.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IEncina>().Send(new GuardedRequest());

        var entry = await CheckAsync(provider);
        entry.Data["decision_audit"].ShouldBe("ok");
    }
}
