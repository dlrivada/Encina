using Encina.Security.AntiTampering;
using Encina.Security.AntiTampering.Abstractions;
using Encina.Security.AntiTampering.Health;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

using HealthCheckResult = Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

#pragma warning disable CA2012 // Use ValueTasks correctly

namespace Encina.UnitTests.Security.AntiTampering;

/// <summary>
/// Unit tests for <see cref="AntiTamperingHealthCheck"/>.
/// </summary>
public sealed class AntiTamperingHealthCheckTests
{
    #region Constructor

    [Fact]
    public void Constructor_NullServiceProvider_ShouldThrowArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new AntiTamperingHealthCheck(null!));
    }

    [Fact]
    public void DefaultName_ShouldBeExpectedValue()
    {
        AntiTamperingHealthCheck.DefaultName.ShouldBe("encina-antitampering");
    }

    #endregion

    #region CheckHealthAsync - Missing Services

    [Fact]
    public async Task CheckHealthAsync_MissingKeyProvider_ShouldReturnUnhealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();
        var healthCheck = new AntiTamperingHealthCheck(sp);

        // Act
        var result = await healthCheck.CheckHealthAsync(null!, CancellationToken.None);

        // Assert
        ((int)result.Status).ShouldBe((int)HealthStatus.Unhealthy);
        result.Description!.ShouldContain("IKeyProvider");
    }

    [Fact]
    public async Task CheckHealthAsync_MissingRequestSigner_ShouldReturnUnhealthy()
    {
        // Arrange
        var keyProvider = Substitute.For<IKeyProvider>();
        var services = new ServiceCollection();
        services.AddSingleton(keyProvider);
        var sp = services.BuildServiceProvider();
        var healthCheck = new AntiTamperingHealthCheck(sp);

        // Act
        var result = await healthCheck.CheckHealthAsync(null!, CancellationToken.None);

        // Assert
        ((int)result.Status).ShouldBe((int)HealthStatus.Unhealthy);
        result.Description!.ShouldContain("IRequestSigner");
    }

    [Fact]
    public async Task CheckHealthAsync_MissingNonceStore_ShouldReturnUnhealthy()
    {
        // Arrange
        var keyProvider = Substitute.For<IKeyProvider>();
        var requestSigner = Substitute.For<IRequestSigner>();
        var services = new ServiceCollection();
        services.AddSingleton(keyProvider);
        services.AddSingleton(requestSigner);
        var sp = services.BuildServiceProvider();
        var healthCheck = new AntiTamperingHealthCheck(sp);

        // Act
        var result = await healthCheck.CheckHealthAsync(null!, CancellationToken.None);

        // Assert
        ((int)result.Status).ShouldBe((int)HealthStatus.Unhealthy);
        result.Description!.ShouldContain("INonceStore");
    }

    #endregion

    #region CheckHealthAsync - Nonce Store Probe Failures

    [Fact]
    public async Task CheckHealthAsync_NonceStoreAddFails_ShouldReturnUnhealthy()
    {
        // Arrange
        var keyProvider = Substitute.For<IKeyProvider>();
        var requestSigner = Substitute.For<IRequestSigner>();
        var nonceStore = Substitute.For<INonceStore>();
        nonceStore.TryAddAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<bool>(false));

        var services = new ServiceCollection();
        services.AddSingleton(keyProvider);
        services.AddSingleton(requestSigner);
        services.AddSingleton(nonceStore);
        var sp = services.BuildServiceProvider();
        var healthCheck = new AntiTamperingHealthCheck(sp);

        // Act
        var result = await healthCheck.CheckHealthAsync(null!, CancellationToken.None);

        // Assert
        ((int)result.Status).ShouldBe((int)HealthStatus.Unhealthy);
        result.Description!.ShouldContain("could not add");
    }

    [Fact]
    public async Task CheckHealthAsync_NonceStoreExistsFails_ShouldReturnUnhealthy()
    {
        // Arrange
        var keyProvider = Substitute.For<IKeyProvider>();
        var requestSigner = Substitute.For<IRequestSigner>();
        var nonceStore = Substitute.For<INonceStore>();
        nonceStore.TryAddAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<bool>(true));
        nonceStore.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<bool>(false));

        var services = new ServiceCollection();
        services.AddSingleton(keyProvider);
        services.AddSingleton(requestSigner);
        services.AddSingleton(nonceStore);
        var sp = services.BuildServiceProvider();
        var healthCheck = new AntiTamperingHealthCheck(sp);

        // Act
        var result = await healthCheck.CheckHealthAsync(null!, CancellationToken.None);

        // Assert
        ((int)result.Status).ShouldBe((int)HealthStatus.Unhealthy);
        result.Description!.ShouldContain("not found on read");
    }

    #endregion

    #region CheckHealthAsync - Healthy

    [Fact]
    public async Task CheckHealthAsync_AllServicesHealthy_ShouldReturnHealthy()
    {
        // Arrange
        var keyProvider = Substitute.For<IKeyProvider>();
        var requestSigner = Substitute.For<IRequestSigner>();
        var nonceStore = Substitute.For<INonceStore>();
        nonceStore.TryAddAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<bool>(true));
        nonceStore.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<bool>(true));

        var services = new ServiceCollection();
        services.AddSingleton(keyProvider);
        services.AddSingleton(requestSigner);
        services.AddSingleton(nonceStore);
        var sp = services.BuildServiceProvider();
        var healthCheck = new AntiTamperingHealthCheck(sp);

        // Act
        var result = await healthCheck.CheckHealthAsync(null!, CancellationToken.None);

        // Assert
        ((int)result.Status).ShouldBe((int)HealthStatus.Healthy);
        result.Data.ShouldContainKey("keyProvider");
        result.Data.ShouldContainKey("requestSigner");
        result.Data.ShouldContainKey("nonceStore");
        result.Data.ShouldContainKey("algorithm");
    }

    #endregion

    #region CheckHealthAsync - Test Keys Verification

    [Fact]
    public async Task CheckHealthAsync_WithTestKeys_KeyRetrievalFails_ShouldReturnUnhealthy()
    {
        // Arrange
        var options = new AntiTamperingOptions();
        options.AddKey("test-key-1", "secret-value");

        var keyProvider = Substitute.For<IKeyProvider>();
        keyProvider.GetKeyAsync("test-key-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, byte[]>>(
                Left(EncinaError.New("Key not found"))));

        var requestSigner = Substitute.For<IRequestSigner>();
        var nonceStore = Substitute.For<INonceStore>();

        var services = new ServiceCollection();
        services.AddSingleton(keyProvider);
        services.AddSingleton(requestSigner);
        services.AddSingleton(nonceStore);
        services.AddSingleton(Options.Create(options));
        var sp = services.BuildServiceProvider();
        var healthCheck = new AntiTamperingHealthCheck(sp);

        // Act
        var result = await healthCheck.CheckHealthAsync(null!, CancellationToken.None);

        // Assert
        ((int)result.Status).ShouldBe((int)HealthStatus.Unhealthy);
        result.Description!.ShouldContain("test-key-1");
    }

    [Fact]
    public async Task CheckHealthAsync_WithTestKeys_KeyRetrievalSucceeds_ShouldIncludeVerifiedKeyId()
    {
        // Arrange
        var options = new AntiTamperingOptions();
        options.AddKey("test-key-1", "secret-value");

        var keyProvider = Substitute.For<IKeyProvider>();
        keyProvider.GetKeyAsync("test-key-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, byte[]>>(
                Right(new byte[] { 1, 2, 3 })));

        var requestSigner = Substitute.For<IRequestSigner>();
        var nonceStore = Substitute.For<INonceStore>();
        nonceStore.TryAddAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<bool>(true));
        nonceStore.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<bool>(true));

        var services = new ServiceCollection();
        services.AddSingleton(keyProvider);
        services.AddSingleton(requestSigner);
        services.AddSingleton(nonceStore);
        services.AddSingleton(Options.Create(options));
        var sp = services.BuildServiceProvider();
        var healthCheck = new AntiTamperingHealthCheck(sp);

        // Act
        var result = await healthCheck.CheckHealthAsync(null!, CancellationToken.None);

        // Assert
        ((int)result.Status).ShouldBe((int)HealthStatus.Healthy);
        result.Data.ShouldContainKey("verifiedKeyId");
        result.Data["verifiedKeyId"].ShouldBe("test-key-1");
    }

    #endregion

    #region CheckHealthAsync - Exception Handling

    [Fact]
    public async Task CheckHealthAsync_ExceptionThrown_ShouldReturnUnhealthy()
    {
        // Arrange: service provider that throws on CreateScope
        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IServiceScopeFactory))
            .Returns(_ => throw new InvalidOperationException("Boom"));

        var healthCheck = new AntiTamperingHealthCheck(sp);

        // Act
        var result = await healthCheck.CheckHealthAsync(null!, CancellationToken.None);

        // Assert
        ((int)result.Status).ShouldBe((int)HealthStatus.Unhealthy);
        result.Description!.ShouldContain("exception");
    }

    #endregion
}
