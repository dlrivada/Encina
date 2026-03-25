using Encina.Compliance.GDPR;
using Encina.Compliance.GDPR.Health;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.GDPR;

/// <summary>
/// Unit tests for <see cref="GDPRHealthCheck"/> and <see cref="ProcessingActivityHealthCheck"/>.
/// </summary>
public sealed class GDPRHealthCheckTests
{
    // ========================================================================
    // GDPRHealthCheck tests
    // ========================================================================

    [Fact]
    public async Task GDPRHealthCheck_WithAllConfigured_ShouldReturnHealthy()
    {
        // Arrange
        var registry = Substitute.For<IProcessingActivityRegistry>();
        var activities = new List<ProcessingActivity> { CreateTestActivity() } as IReadOnlyList<ProcessingActivity>;
        registry.GetAllActivitiesAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ProcessingActivity>>(activities));

        var validator = Substitute.For<IGDPRComplianceValidator>();

        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<GDPROptions>(options =>
        {
            options.ControllerName = "Test Corp";
            options.ControllerEmail = "dpo@test.com";
        });
        services.AddScoped(_ => registry);
        services.AddScoped(_ => validator);

        var provider = services.BuildServiceProvider();
        var healthCheck = new GDPRHealthCheck(
            provider,
            provider.GetRequiredService<ILogger<GDPRHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("fully configured");
    }

    [Fact]
    public async Task GDPRHealthCheck_WithoutRegistry_ButWithOptions_ShouldReturnUnhealthy()
    {
        // Arrange - options resolve with defaults, but registry is missing
        var services = new ServiceCollection();
        services.AddLogging();
        // IOptions<GDPROptions> auto-resolves with defaults; registry is not registered

        var provider = services.BuildServiceProvider();
        var healthCheck = new GDPRHealthCheck(
            provider,
            provider.GetRequiredService<ILogger<GDPRHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("IProcessingActivityRegistry");
    }

    [Fact]
    public async Task GDPRHealthCheck_WithoutRegistry_ShouldReturnUnhealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<GDPROptions>(options =>
        {
            options.ControllerName = "Test";
            options.ControllerEmail = "test@test.com";
        });

        var provider = services.BuildServiceProvider();
        var healthCheck = new GDPRHealthCheck(
            provider,
            provider.GetRequiredService<ILogger<GDPRHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("IProcessingActivityRegistry");
    }

    [Fact]
    public async Task GDPRHealthCheck_WithMissingControllerName_ShouldReturnDegraded()
    {
        // Arrange
        var registry = Substitute.For<IProcessingActivityRegistry>();
        var activities = new List<ProcessingActivity> { CreateTestActivity() } as IReadOnlyList<ProcessingActivity>;
        registry.GetAllActivitiesAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ProcessingActivity>>(activities));

        var validator = Substitute.For<IGDPRComplianceValidator>();

        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<GDPROptions>(_ =>
        {
            // Missing ControllerName and ControllerEmail
        });
        services.AddScoped(_ => registry);
        services.AddScoped(_ => validator);

        var provider = services.BuildServiceProvider();
        var healthCheck = new GDPRHealthCheck(
            provider,
            provider.GetRequiredService<ILogger<GDPRHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("ControllerName");
    }

    [Fact]
    public async Task GDPRHealthCheck_WithEmptyRegistry_ShouldReturnDegraded()
    {
        // Arrange
        var registry = Substitute.For<IProcessingActivityRegistry>();
        var emptyActivities = new List<ProcessingActivity>() as IReadOnlyList<ProcessingActivity>;
        registry.GetAllActivitiesAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ProcessingActivity>>(emptyActivities));

        var validator = Substitute.For<IGDPRComplianceValidator>();

        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<GDPROptions>(options =>
        {
            options.ControllerName = "Test";
            options.ControllerEmail = "test@test.com";
        });
        services.AddScoped(_ => registry);
        services.AddScoped(_ => validator);

        var provider = services.BuildServiceProvider();
        var healthCheck = new GDPRHealthCheck(
            provider,
            provider.GetRequiredService<ILogger<GDPRHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("No processing activities");
    }

    [Fact]
    public async Task GDPRHealthCheck_WithMissingValidator_ShouldReturnDegraded()
    {
        // Arrange
        var registry = Substitute.For<IProcessingActivityRegistry>();
        var activities = new List<ProcessingActivity> { CreateTestActivity() } as IReadOnlyList<ProcessingActivity>;
        registry.GetAllActivitiesAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ProcessingActivity>>(activities));

        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<GDPROptions>(options =>
        {
            options.ControllerName = "Test";
            options.ControllerEmail = "test@test.com";
        });
        services.AddScoped(_ => registry);
        // Do NOT register IGDPRComplianceValidator

        var provider = services.BuildServiceProvider();
        var healthCheck = new GDPRHealthCheck(
            provider,
            provider.GetRequiredService<ILogger<GDPRHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("IGDPRComplianceValidator");
    }

    [Fact]
    public void GDPRHealthCheck_DefaultName_ShouldBeExpectedValue()
    {
        GDPRHealthCheck.DefaultName.ShouldBe("encina-gdpr");
    }

    [Fact]
    public void GDPRHealthCheck_Tags_ShouldContainExpectedValues()
    {
        var tags = GDPRHealthCheck.Tags.ToList();
        tags.ShouldContain("encina");
        tags.ShouldContain("gdpr");
        tags.ShouldContain("compliance");
    }

    // ========================================================================
    // ProcessingActivityHealthCheck tests
    // ========================================================================

    [Fact]
    public async Task ProcessingActivityHealthCheck_WithRegistryAndActivities_ShouldReturnHealthy()
    {
        // Arrange
        var registry = Substitute.For<IProcessingActivityRegistry>();
        var activities = new List<ProcessingActivity> { CreateTestActivity() } as IReadOnlyList<ProcessingActivity>;
        registry.GetAllActivitiesAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ProcessingActivity>>(activities));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => registry);

        var provider = services.BuildServiceProvider();
        var healthCheck = new ProcessingActivityHealthCheck(
            provider,
            provider.GetRequiredService<ILogger<ProcessingActivityHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("fully configured");
    }

    [Fact]
    public async Task ProcessingActivityHealthCheck_WithoutRegistry_ShouldReturnUnhealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var provider = services.BuildServiceProvider();
        var healthCheck = new ProcessingActivityHealthCheck(
            provider,
            provider.GetRequiredService<ILogger<ProcessingActivityHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("IProcessingActivityRegistry");
    }

    [Fact]
    public async Task ProcessingActivityHealthCheck_WithEmptyRegistry_ShouldReturnDegraded()
    {
        // Arrange
        var registry = Substitute.For<IProcessingActivityRegistry>();
        var emptyActivities = new List<ProcessingActivity>() as IReadOnlyList<ProcessingActivity>;
        registry.GetAllActivitiesAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ProcessingActivity>>(emptyActivities));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => registry);

        var provider = services.BuildServiceProvider();
        var healthCheck = new ProcessingActivityHealthCheck(
            provider,
            provider.GetRequiredService<ILogger<ProcessingActivityHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("No processing activities");
    }

    [Fact]
    public async Task ProcessingActivityHealthCheck_WhenRegistryQueryFails_ShouldReturnDegraded()
    {
        // Arrange
        var registry = Substitute.For<IProcessingActivityRegistry>();
        var error = EncinaErrors.Create("query.failed", "DB error");
        registry.GetAllActivitiesAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IReadOnlyList<ProcessingActivity>>(error));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => registry);

        var provider = services.BuildServiceProvider();
        var healthCheck = new ProcessingActivityHealthCheck(
            provider,
            provider.GetRequiredService<ILogger<ProcessingActivityHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

#pragma warning disable CA2201, CA2012
    [Fact]
    public async Task ProcessingActivityHealthCheck_WhenRegistryThrows_ShouldReturnDegraded()
    {
        // Arrange
        var registry = Substitute.For<IProcessingActivityRegistry>();
        registry.GetAllActivitiesAsync(Arg.Any<CancellationToken>())
            .Throws(new Exception("Connection refused"));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => registry);

        var provider = services.BuildServiceProvider();
        var healthCheck = new ProcessingActivityHealthCheck(
            provider,
            provider.GetRequiredService<ILogger<ProcessingActivityHealthCheck>>());

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("Failed to access");
    }
#pragma warning restore CA2201, CA2012

    [Fact]
    public void ProcessingActivityHealthCheck_DefaultName_ShouldBeExpectedValue()
    {
        ProcessingActivityHealthCheck.DefaultName.ShouldBe("encina-processing-activity");
    }

    [Fact]
    public void ProcessingActivityHealthCheck_Tags_ShouldContainExpectedValues()
    {
        var tags = ProcessingActivityHealthCheck.Tags.ToList();
        tags.ShouldContain("encina");
        tags.ShouldContain("gdpr");
        tags.ShouldContain("processing-activity");
    }

    [Fact]
    public void ProcessingActivityHealthCheck_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new ProcessingActivityHealthCheck(
            null!, Substitute.For<ILogger<ProcessingActivityHealthCheck>>());
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void ProcessingActivityHealthCheck_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ProcessingActivityHealthCheck(
            Substitute.For<IServiceProvider>(), null!);
        act.ShouldThrow<ArgumentNullException>();
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private static ProcessingActivity CreateTestActivity() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Activity",
        Purpose = "Testing",
        LawfulBasis = global::Encina.Compliance.GDPR.LawfulBasis.LegitimateInterests,
        CategoriesOfDataSubjects = ["Users"],
        CategoriesOfPersonalData = ["Email"],
        Recipients = ["Internal"],
        RetentionPeriod = TimeSpan.FromDays(365),
        SecurityMeasures = "Encryption",
        RequestType = typeof(string),
        CreatedAtUtc = DateTimeOffset.UtcNow,
        LastUpdatedAtUtc = DateTimeOffset.UtcNow
    };
}
