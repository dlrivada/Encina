#pragma warning disable CA2012 // Use ValueTasks correctly - required for NSubstitute mock setup

using Encina.Compliance.Attestation.Abstractions;
using Encina.Compliance.Attestation.Health;
using Encina.Compliance.Attestation.Model;
using LanguageExt;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Attestation;

/// <summary>
/// Unit tests for <see cref="AttestationHealthCheck"/>.
/// </summary>
public sealed class AttestationHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_AttestSucceeds_ReturnsHealthy()
    {
        var provider = Substitute.For<IAuditAttestationProvider>();
        provider.ProviderName.Returns("TestProvider");
        provider.AttestAsync(Arg.Any<AuditRecord>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, AttestationReceipt>(new AttestationReceipt
                {
                    AttestationId = Guid.NewGuid(),
                    AuditRecordId = Guid.NewGuid(),
                    ContentHash = "hash",
                    ProviderName = "TestProvider",
                    Signature = "sig",
                    AttestedAtUtc = DateTimeOffset.UtcNow
                })));

        var sut = new AttestationHealthCheck(provider, NullLogger<AttestationHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("operational");
    }

    [Fact]
    public async Task CheckHealthAsync_AttestFails_DoesNotLeakErrorMessage()
    {
        const string sentinel = "sensitive-connection-string-sentinel";
        var provider = Substitute.For<IAuditAttestationProvider>();
        provider.ProviderName.Returns("TestProvider");
        provider.AttestAsync(Arg.Any<AuditRecord>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Left<EncinaError, AttestationReceipt>(EncinaErrors.Create("attest.failed", sentinel))));

        var sut = new AttestationHealthCheck(provider, NullLogger<AttestationHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldNotContain(sentinel);
        result.Description!.ShouldContain("attest.failed");
    }

    [Fact]
    public async Task CheckHealthAsync_AttestThrows_DoesNotLeakExceptionMessage()
    {
        const string sentinel = "sensitive-connection-string-sentinel";
        var provider = Substitute.For<IAuditAttestationProvider>();
        provider.ProviderName.Returns("TestProvider");
        provider.AttestAsync(Arg.Any<AuditRecord>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, AttestationReceipt>>>(_ =>
                throw new InvalidOperationException(sentinel));

        var sut = new AttestationHealthCheck(provider, NullLogger<AttestationHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldNotContain(sentinel);
        result.Description!.ShouldContain(nameof(InvalidOperationException));
    }
}
