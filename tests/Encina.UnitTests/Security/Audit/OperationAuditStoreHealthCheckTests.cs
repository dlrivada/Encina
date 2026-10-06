#pragma warning disable CA2012 // Use ValueTasks correctly - required for NSubstitute mock setup

using Encina.Messaging.Health;
using Encina.Security.Audit;
using Encina.Security.Audit.Health;
using LanguageExt;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Unit tests for <see cref="OperationAuditStoreHealthCheck"/>.
/// </summary>
public sealed class OperationAuditStoreHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_StoreReturnsError_DoesNotLeakErrorMessage()
    {
        const string sentinel = "sensitive-connection-string-sentinel";
        var store = Substitute.For<IOperationAuditStore>();
        store.GetByEntityAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Left<EncinaError, IReadOnlyList<OperationAuditEntry>>(
                    EncinaErrors.Create("audit.store.error", sentinel))));

        var sut = new OperationAuditStoreHealthCheck(store);

        var result = await sut.CheckHealthAsync(CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldNotContain(sentinel);
        result.Description!.ShouldContain("audit.store.error");
        result.Data["error"].ShouldBe("audit.store.error");
    }

    [Fact]
    public async Task CheckHealthAsync_StoreAccessible_ReturnsHealthy()
    {
        var store = Substitute.For<IOperationAuditStore>();
        store.GetByEntityAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<OperationAuditEntry>>(
                    System.Array.Empty<OperationAuditEntry>())));

        var sut = new OperationAuditStoreHealthCheck(store);

        var result = await sut.CheckHealthAsync(CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }
}
