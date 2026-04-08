#pragma warning disable CA2012 // Use ValueTasks correctly - NSubstitute .Returns() pattern with ValueTask is safe

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Health;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.ReadModels;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using LanguageExt;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Extended guard tests for <see cref="ProcessorAgreementHealthCheck"/> that exercise
/// the CheckHealthAsync method paths to cover more executable lines.
/// </summary>
public sealed class ProcessorAgreementHealthCheckExtendedGuardTests
{
    [Fact]
    public void DefaultName_IsExpectedValue()
    {
        ProcessorAgreementHealthCheck.DefaultName.ShouldBe("encina-processor-agreements");
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        var tags = ProcessorAgreementHealthCheck.Tags.ToList();

        tags.ShouldContain("encina");
        tags.ShouldContain("gdpr");
        tags.ShouldContain("processor-agreements");
        tags.ShouldContain("dpa");
        tags.ShouldContain("compliance");
        tags.ShouldContain("ready");
    }

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = NullLogger<ProcessorAgreementHealthCheck>.Instance;

        var act = () => new ProcessorAgreementHealthCheck(serviceProvider, logger);

        Should.NotThrow(act);
    }

    private static (ProcessorAgreementHealthCheck HealthCheck, HealthCheckContext Context) CreateHealthCheckWithScope(
        IServiceProvider scopedProvider)
    {
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(scopedProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider
            .GetService(typeof(IServiceScopeFactory))
            .Returns(scopeFactory);

        var logger = NullLogger<ProcessorAgreementHealthCheck>.Instance;
        var healthCheck = new ProcessorAgreementHealthCheck(serviceProvider, logger);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                ProcessorAgreementHealthCheck.DefaultName,
                healthCheck,
                failureStatus: null,
                tags: null)
        };

        return (healthCheck, context);
    }

    [Fact]
    public async Task CheckHealthAsync_NoOptionsConfigured_ReturnsUnhealthy()
    {
        var scopedProvider = Substitute.For<IServiceProvider>();
        scopedProvider
            .GetService(typeof(IOptions<ProcessorAgreementOptions>))
            .Returns((object?)null);

        var (healthCheck, context) = CreateHealthCheckWithScope(scopedProvider);

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("ProcessorAgreementOptions are not configured");
    }

    [Fact]
    public async Task CheckHealthAsync_NoProcessorService_ReturnsUnhealthy()
    {
        var options = new ProcessorAgreementOptions
        {
            EnforcementMode = ProcessorAgreementEnforcementMode.Block
        };
        var optionsWrapper = Microsoft.Extensions.Options.Options.Create(options);

        var scopedProvider = Substitute.For<IServiceProvider>();
        scopedProvider
            .GetService(typeof(IOptions<ProcessorAgreementOptions>))
            .Returns(optionsWrapper);
        scopedProvider
            .GetService(typeof(IProcessorService))
            .Returns((object?)null);

        var (healthCheck, context) = CreateHealthCheckWithScope(scopedProvider);

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("IProcessorService is not registered");
    }

    [Fact]
    public async Task CheckHealthAsync_NoDPAService_ReturnsUnhealthy()
    {
        var options = new ProcessorAgreementOptions();
        var optionsWrapper = Microsoft.Extensions.Options.Options.Create(options);
        var processorService = Substitute.For<IProcessorService>();

        var scopedProvider = Substitute.For<IServiceProvider>();
        scopedProvider
            .GetService(typeof(IOptions<ProcessorAgreementOptions>))
            .Returns(optionsWrapper);
        scopedProvider
            .GetService(typeof(IProcessorService))
            .Returns(processorService);
        scopedProvider
            .GetService(typeof(IDPAService))
            .Returns((object?)null);

        var (healthCheck, context) = CreateHealthCheckWithScope(scopedProvider);

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("IDPAService is not registered");
    }

    [Fact]
    public async Task CheckHealthAsync_AllServicesPresent_NoExpired_ReturnsHealthy()
    {
        var options = new ProcessorAgreementOptions();
        var optionsWrapper = Microsoft.Extensions.Options.Options.Create(options);
        var processorService = Substitute.For<IProcessorService>();
        var dpaService = Substitute.For<IDPAService>();

        var emptyList = (IReadOnlyList<DPAReadModel>)Array.Empty<DPAReadModel>();
        dpaService
            .GetDPAsByStatusAsync(DPAStatus.Expired, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, IReadOnlyList<DPAReadModel>>>(
                Either<EncinaError, IReadOnlyList<DPAReadModel>>.Right(emptyList)));

        var scopedProvider = Substitute.For<IServiceProvider>();
        scopedProvider
            .GetService(typeof(IOptions<ProcessorAgreementOptions>))
            .Returns(optionsWrapper);
        scopedProvider
            .GetService(typeof(IProcessorService))
            .Returns(processorService);
        scopedProvider
            .GetService(typeof(IDPAService))
            .Returns(dpaService);

        var (healthCheck, context) = CreateHealthCheckWithScope(scopedProvider);

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("fully configured");
    }

    [Fact]
    public async Task CheckHealthAsync_ExpiredDPAsExist_ReturnsDegraded()
    {
        var options = new ProcessorAgreementOptions();
        var optionsWrapper = Microsoft.Extensions.Options.Options.Create(options);
        var processorService = Substitute.For<IProcessorService>();
        var dpaService = Substitute.For<IDPAService>();

        var now = DateTimeOffset.UtcNow;
        var terms = new DPAMandatoryTerms
        {
            ProcessOnDocumentedInstructions = true,
            ConfidentialityObligations = true,
            SecurityMeasures = true,
            SubProcessorRequirements = true,
            DataSubjectRightsAssistance = true,
            ComplianceAssistance = true,
            DataDeletionOrReturn = true,
            AuditRights = true
        };

        var expiredDPAs = (IReadOnlyList<DPAReadModel>)new List<DPAReadModel>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProcessorId = Guid.NewGuid(),
                Status = DPAStatus.Expired,
                MandatoryTerms = terms,
                HasSCCs = false,
                ProcessingPurposes = ["Analytics"],
                SignedAtUtc = now.AddYears(-2),
                CreatedAtUtc = now.AddYears(-2),
                LastModifiedAtUtc = now.AddDays(-30)
            }
        };

        dpaService
            .GetDPAsByStatusAsync(DPAStatus.Expired, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, IReadOnlyList<DPAReadModel>>>(
                Either<EncinaError, IReadOnlyList<DPAReadModel>>.Right(expiredDPAs)));

        var scopedProvider = Substitute.For<IServiceProvider>();
        scopedProvider
            .GetService(typeof(IOptions<ProcessorAgreementOptions>))
            .Returns(optionsWrapper);
        scopedProvider
            .GetService(typeof(IProcessorService))
            .Returns(processorService);
        scopedProvider
            .GetService(typeof(IDPAService))
            .Returns(dpaService);

        var (healthCheck, context) = CreateHealthCheckWithScope(scopedProvider);

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("warnings");
    }

    [Fact]
    public async Task CheckHealthAsync_DPAServiceReturnsError_ReturnsDegraded()
    {
        var options = new ProcessorAgreementOptions();
        var optionsWrapper = Microsoft.Extensions.Options.Options.Create(options);
        var processorService = Substitute.For<IProcessorService>();
        var dpaService = Substitute.For<IDPAService>();

        var error = EncinaErrors.Create("store.unavailable", "Store unavailable");
        dpaService
            .GetDPAsByStatusAsync(DPAStatus.Expired, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, IReadOnlyList<DPAReadModel>>>(
                Either<EncinaError, IReadOnlyList<DPAReadModel>>.Left(error)));

        var scopedProvider = Substitute.For<IServiceProvider>();
        scopedProvider
            .GetService(typeof(IOptions<ProcessorAgreementOptions>))
            .Returns(optionsWrapper);
        scopedProvider
            .GetService(typeof(IProcessorService))
            .Returns(processorService);
        scopedProvider
            .GetService(typeof(IDPAService))
            .Returns(dpaService);

        var (healthCheck, context) = CreateHealthCheckWithScope(scopedProvider);

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("Unable to query expired DPAs");
    }

    [Fact]
    public async Task CheckHealthAsync_DPAServiceThrows_ReturnsDegradedWithWarning()
    {
        var options = new ProcessorAgreementOptions();
        var optionsWrapper = Microsoft.Extensions.Options.Options.Create(options);
        var processorService = Substitute.For<IProcessorService>();
        var dpaService = Substitute.For<IDPAService>();

        dpaService
            .GetDPAsByStatusAsync(DPAStatus.Expired, Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, IReadOnlyList<DPAReadModel>>>>(
                _ => throw new InvalidOperationException("Database connection lost"));

        var scopedProvider = Substitute.For<IServiceProvider>();
        scopedProvider
            .GetService(typeof(IOptions<ProcessorAgreementOptions>))
            .Returns(optionsWrapper);
        scopedProvider
            .GetService(typeof(IProcessorService))
            .Returns(processorService);
        scopedProvider
            .GetService(typeof(IDPAService))
            .Returns(dpaService);

        var (healthCheck, context) = CreateHealthCheckWithScope(scopedProvider);

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("Database connection lost");
    }
}
