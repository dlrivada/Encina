#pragma warning disable CA2012 // Use ValueTasks correctly (NSubstitute Returns with ValueTask)

using Encina;
using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Health;
using Encina.Compliance.NIS2.Model;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.NIS2;

/// <summary>
/// Unit tests for <see cref="NIS2ComplianceHealthCheck"/>.
/// </summary>
public class NIS2ComplianceHealthCheckTests
{
    #region Helpers

    private static (NIS2ComplianceHealthCheck healthCheck, INIS2ComplianceValidator validator) CreateSut()
    {
        var mockValidator = Substitute.For<INIS2ComplianceValidator>();

        var scope = Substitute.For<IServiceScope>();
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
        scope.ServiceProvider.GetService(typeof(INIS2ComplianceValidator)).Returns(mockValidator);

        // GetRequiredService calls GetService internally
        var scopedProvider = scope.ServiceProvider;
        scopedProvider.GetService(typeof(INIS2ComplianceValidator)).Returns(mockValidator);

        var healthCheck = new NIS2ComplianceHealthCheck(
            serviceProvider,
            NullLogger<NIS2ComplianceHealthCheck>.Instance);

        return (healthCheck, mockValidator);
    }

    private static NIS2ComplianceResult CreateCompliantResult() =>
        NIS2ComplianceResult.Create(
            NIS2EntityType.Essential,
            NIS2Sector.Energy,
            Enum.GetValues<NIS2Measure>()
                .Select(m => NIS2MeasureResult.Satisfied(m, "OK"))
                .ToList(),
            DateTimeOffset.UtcNow);

    private static NIS2ComplianceResult CreatePartialResult() =>
        NIS2ComplianceResult.Create(
            NIS2EntityType.Essential,
            NIS2Sector.Energy,
            [
                NIS2MeasureResult.Satisfied(NIS2Measure.RiskAnalysisAndSecurityPolicies, "OK"),
                NIS2MeasureResult.NotSatisfied(NIS2Measure.IncidentHandling, "Missing", ["Fix"]),
                NIS2MeasureResult.Satisfied(NIS2Measure.BusinessContinuity, "OK")
            ],
            DateTimeOffset.UtcNow);

    #endregion

    #region CheckHealthAsync_FullyCompliant

    [Fact]
    public async Task CheckHealthAsync_FullyCompliant_ShouldReturnHealthy()
    {
        // Arrange
        var (healthCheck, validator) = CreateSut();
        validator.ValidateAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, NIS2ComplianceResult>>(
                Right<EncinaError, NIS2ComplianceResult>(CreateCompliantResult())));

        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                NIS2ComplianceHealthCheck.DefaultName,
                healthCheck,
                HealthStatus.Unhealthy,
                null)
        };

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    #endregion

    #region CheckHealthAsync_PartialCompliance

    [Fact]
    public async Task CheckHealthAsync_PartialCompliance_ShouldReturnDegraded()
    {
        // Arrange
        var (healthCheck, validator) = CreateSut();
        validator.ValidateAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, NIS2ComplianceResult>>(
                Right<EncinaError, NIS2ComplianceResult>(CreatePartialResult())));

        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                NIS2ComplianceHealthCheck.DefaultName,
                healthCheck,
                HealthStatus.Unhealthy,
                null)
        };

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    #endregion

    #region CheckHealthAsync_ValidationError

    [Fact]
    public async Task CheckHealthAsync_ValidationError_ShouldReturnUnhealthy()
    {
        // Arrange
        var (healthCheck, validator) = CreateSut();
        validator.ValidateAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, NIS2ComplianceResult>>(
                Left<EncinaError, NIS2ComplianceResult>(
                    EncinaError.New("NIS2 compliance validation failed"))));

        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                NIS2ComplianceHealthCheck.DefaultName,
                healthCheck,
                HealthStatus.Unhealthy,
                null)
        };

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    #endregion
}
