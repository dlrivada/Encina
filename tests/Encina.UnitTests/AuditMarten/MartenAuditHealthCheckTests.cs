#pragma warning disable CA2012 // NSubstitute ValueTask stubbing pattern

using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Encina.Audit.Marten.Health;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Unit tests for <see cref="MartenAuditHealthCheck"/> DI-based health check.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class MartenAuditHealthCheckTests
{
    private static readonly HealthCheckContext DummyContext = new()
    {
        Registration = new HealthCheckRegistration(
            MartenAuditHealthCheck.DefaultName,
            Substitute.For<IHealthCheck>(),
            HealthStatus.Unhealthy,
            tags: null)
    };

    [Fact]
    public void Constants_DefaultName_IsCorrect()
    {
        MartenAuditHealthCheck.DefaultName.ShouldBe("encina-audit-marten");
    }

    [Fact]
    public async Task CheckHealthAsync_NoKeyProviderRegistered_ReturnsUnhealthy()
    {
        // Arrange — no ITemporalKeyProvider in DI
        var services = new ServiceCollection().BuildServiceProvider();
        var sut = new MartenAuditHealthCheck(services);

        // Act
        var result = await sut.CheckHealthAsync(DummyContext);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("ITemporalKeyProvider");
    }

    [Fact]
    public async Task CheckHealthAsync_KeyProviderWithActiveKeys_ReturnsHealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero));
        var keyProvider = new InMemoryTemporalKeyProvider(timeProvider, NullLogger<InMemoryTemporalKeyProvider>.Instance);
        await keyProvider.GetOrCreateKeyAsync("2026-03");

        services.AddSingleton<ITemporalKeyProvider>(keyProvider);
        var sp = services.BuildServiceProvider();
        var sut = new MartenAuditHealthCheck(sp);

        // Act
        var result = await sut.CheckHealthAsync(DummyContext);

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data.ShouldContainKey("active_key_count");
        result.Data.ShouldContainKey("provider_type");
    }

    [Fact]
    public async Task CheckHealthAsync_KeyProviderWithNoKeys_ReturnsDegraded()
    {
        // Arrange
        var services = new ServiceCollection();
        var timeProvider = new FakeTimeProvider();
        var keyProvider = new InMemoryTemporalKeyProvider(timeProvider, NullLogger<InMemoryTemporalKeyProvider>.Instance);

        services.AddSingleton<ITemporalKeyProvider>(keyProvider);
        var sp = services.BuildServiceProvider();
        var sut = new MartenAuditHealthCheck(sp);

        // Act
        var result = await sut.CheckHealthAsync(DummyContext);

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_KeyProviderReturnsLeft_ReturnsDegraded()
    {
        // Arrange
        var services = new ServiceCollection();
        var keyProvider = Substitute.For<ITemporalKeyProvider>();
        keyProvider.GetActiveKeysAsync(Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, IReadOnlyList<TemporalKeyInfo>>>(
                Left<EncinaError, IReadOnlyList<TemporalKeyInfo>>(
                    MartenAuditErrors.StoreUnavailable("unreachable"))));

        services.AddSingleton(keyProvider);
        var sp = services.BuildServiceProvider();
        var sut = new MartenAuditHealthCheck(sp);

        // Act
        var result = await sut.CheckHealthAsync(DummyContext);

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Data.ShouldContainKey("error");
    }

    [Fact]
    public async Task CheckHealthAsync_KeyProviderThrows_ReturnsUnhealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        var keyProvider = Substitute.For<ITemporalKeyProvider>();
        keyProvider.GetActiveKeysAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, IReadOnlyList<TemporalKeyInfo>>>>(
                _ => throw new InvalidOperationException("boom"));

        services.AddSingleton(keyProvider);
        var sp = services.BuildServiceProvider();
        var sut = new MartenAuditHealthCheck(sp);

        // Act
        var result = await sut.CheckHealthAsync(DummyContext);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldNotBeNull();
    }
}
