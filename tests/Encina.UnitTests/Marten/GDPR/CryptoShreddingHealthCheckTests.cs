using Encina.Marten.GDPR.Abstractions;
using Encina.Marten.GDPR.Health;
using Encina.Security.Encryption.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Marten.GDPR;

/// <summary>
/// Unit tests for <see cref="CryptoShreddingHealthCheck"/>.
/// </summary>
public sealed class CryptoShreddingHealthCheckTests
{
    private static readonly HealthCheckContext Context = new();

    [Fact]
    public void Constructor_NullServiceProvider_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CryptoShreddingHealthCheck(null!, NullLogger<CryptoShreddingHealthCheck>.Instance));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CryptoShreddingHealthCheck(Substitute.For<IServiceProvider>(), null!));
    }

    [Fact]
    public async Task CheckHealthAsync_WithoutFieldEncryptor_ReturnsUnhealthy()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var sut = new CryptoShreddingHealthCheck(sp, NullLogger<CryptoShreddingHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(Context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("IFieldEncryptor");
    }

    [Fact]
    public async Task CheckHealthAsync_WithoutSubjectKeyProvider_ReturnsUnhealthy()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IFieldEncryptor>());
        var sut = new CryptoShreddingHealthCheck(
            services.BuildServiceProvider(), NullLogger<CryptoShreddingHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(Context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("ISubjectKeyProvider");
    }

    [Fact]
    public async Task CheckHealthAsync_WithBothServices_ReportsKeyProviderType()
    {
        // The property cache is process-wide, so the status is Healthy or Degraded depending on
        // which other tests ran first; both carry the key provider type.
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IFieldEncryptor>());
        services.AddSingleton(Substitute.For<ISubjectKeyProvider>());
        var sut = new CryptoShreddingHealthCheck(
            services.BuildServiceProvider(), NullLogger<CryptoShreddingHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(Context);

        result.Status.ShouldBeOneOf(HealthStatus.Healthy, HealthStatus.Degraded);
        result.Data.ShouldContainKey("keyProviderType");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenResolutionThrows_ReportsOnlyTheExceptionType()
    {
        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IServiceScopeFactory))
            .Returns(_ => throw new InvalidOperationException("subject-4711 key store unreachable"));
        var sut = new CryptoShreddingHealthCheck(sp, NullLogger<CryptoShreddingHealthCheck>.Instance);

        var result = await sut.CheckHealthAsync(Context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain(nameof(InvalidOperationException));
        result.Description!.ShouldNotContain("subject-4711");
        result.Exception.ShouldBeNull();
    }
}
