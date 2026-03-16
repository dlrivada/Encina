#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.Scheduling;
using Encina.Compliance.ProcessorAgreements.Services;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Compliance.ProcessorAgreements;

/// <summary>
/// Integration tests for the Processor Agreements pipeline verifying DI registration,
/// options configuration, health check registration, TryAdd semantics, and pipeline
/// enforcement behavior using in-memory DI and mocked services.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ProcessorAgreementPipelineIntegrationTests
{
    #region DI Registration

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersIProcessorService_AsScoped()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IProcessorService));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        descriptor.ImplementationType.Should().Be(typeof(DefaultProcessorService));
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersIDPAService_AsScoped()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IDPAService));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        descriptor.ImplementationType.Should().Be(typeof(DefaultDPAService));
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersPipelineBehavior_AsTransient()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements();

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Transient);
        descriptor.ImplementationType.Should().Be(typeof(ProcessorValidationPipelineBehavior<,>));
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersExpirationHandler_AsTransient()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements();

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ICommandHandler<CheckDPAExpirationCommand, Unit>));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Transient);
        descriptor.ImplementationType.Should().Be(typeof(CheckDPAExpirationHandler));
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersTimeProvider_AsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TimeProvider));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersOptions_Resolvable()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements();
        var provider = services.BuildServiceProvider();

        var options = provider.GetService<IOptions<ProcessorAgreementOptions>>();
        options.Should().NotBeNull();
        options!.Value.Should().NotBeNull();
    }

    #endregion

    #region Options Configuration

    [Fact]
    public void DefaultOptions_HaveExpectedValues()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements();
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<ProcessorAgreementOptions>>().Value;

        options.EnforcementMode.Should().Be(ProcessorAgreementEnforcementMode.Warn);
        options.BlockWithoutValidDPA.Should().BeFalse();
        options.MaxSubProcessorDepth.Should().Be(3);
        options.EnableExpirationMonitoring.Should().BeFalse();
        options.ExpirationCheckInterval.Should().Be(TimeSpan.FromHours(1));
        options.ExpirationWarningDays.Should().Be(30);
        options.TrackAuditTrail.Should().BeTrue();
        options.AddHealthCheck.Should().BeFalse();
    }

    [Fact]
    public void CustomConfigureAction_IsApplied()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements(opts =>
        {
            opts.EnforcementMode = ProcessorAgreementEnforcementMode.Block;
            opts.MaxSubProcessorDepth = 5;
            opts.ExpirationWarningDays = 60;
        });
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<ProcessorAgreementOptions>>().Value;

        options.EnforcementMode.Should().Be(ProcessorAgreementEnforcementMode.Block);
        options.MaxSubProcessorDepth.Should().Be(5);
        options.ExpirationWarningDays.Should().Be(60);
    }

    [Fact]
    public void BlockWithoutValidDPA_SetsEnforcementModeToBlock()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements(opts =>
        {
            opts.BlockWithoutValidDPA = true;
        });
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<ProcessorAgreementOptions>>().Value;

        options.EnforcementMode.Should().Be(ProcessorAgreementEnforcementMode.Block);
        options.BlockWithoutValidDPA.Should().BeTrue();
    }

    #endregion

    #region Health Check Registration

    [Fact]
    public void HealthCheck_NotRegisteredByDefault()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements();

        // HealthCheckService is only added when AddHealthChecks() is called
        var healthCheckDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(HealthCheckService));
        healthCheckDescriptor.Should().BeNull();
    }

    [Fact]
    public void HealthCheck_RegisteredWhenEnabled()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements(opts =>
        {
            opts.AddHealthCheck = true;
        });

        // When AddHealthCheck = true, AddHealthChecks().AddCheck<T>() is called,
        // which registers HealthCheckService
        var healthCheckDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(HealthCheckService));
        healthCheckDescriptor.Should().NotBeNull();
    }

    #endregion

    #region TryAdd Semantics

    [Fact]
    public void CustomIDPAService_PreservedWhenRegisteredBefore()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Register custom implementation before AddEncinaProcessorAgreements
        services.AddScoped<IDPAService>(_ => Substitute.For<IDPAService>());
        services.AddEncinaProcessorAgreements();

        var descriptor = services.First(d => d.ServiceType == typeof(IDPAService));
        descriptor.ImplementationType.Should().NotBe(typeof(DefaultDPAService));
    }

    [Fact]
    public void CustomIProcessorService_PreservedWhenRegisteredBefore()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Register custom implementation before AddEncinaProcessorAgreements
        services.AddScoped<IProcessorService>(_ => Substitute.For<IProcessorService>());
        services.AddEncinaProcessorAgreements();

        var descriptor = services.First(d => d.ServiceType == typeof(IProcessorService));
        descriptor.ImplementationType.Should().NotBe(typeof(DefaultProcessorService));
    }

    #endregion

    #region Pipeline Behavior with Mocked Services

    [RequiresProcessor(ProcessorId = "00000000-0000-0000-0000-000000000001")]
    private sealed record TestCommand(string Data) : ICommand<string>;

    [Fact]
    public async Task DisabledMode_SkipsValidation()
    {
        var dpaService = Substitute.For<IDPAService>();
        var options = Options.Create(new ProcessorAgreementOptions
        {
            EnforcementMode = ProcessorAgreementEnforcementMode.Disabled
        });
        var logger = NullLogger<ProcessorValidationPipelineBehavior<TestCommand, string>>.Instance;

        var behavior = new ProcessorValidationPipelineBehavior<TestCommand, string>(
            dpaService, options, logger);

        var request = new TestCommand("test");
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("success"));

        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        result.IsRight.Should().BeTrue();
        ((string)result).Should().Be("success");
        await dpaService.DidNotReceive().HasValidDPAAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BlockMode_WithValidDPA_AllowsThrough()
    {
        var processorId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var dpaService = Substitute.For<IDPAService>();
        dpaService.HasValidDPAAsync(processorId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, bool>>(Right<EncinaError, bool>(true)));

        var options = Options.Create(new ProcessorAgreementOptions
        {
            EnforcementMode = ProcessorAgreementEnforcementMode.Block
        });
        var logger = NullLogger<ProcessorValidationPipelineBehavior<TestCommand, string>>.Instance;

        var behavior = new ProcessorValidationPipelineBehavior<TestCommand, string>(
            dpaService, options, logger);

        var request = new TestCommand("test");
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("success"));

        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        result.IsRight.Should().BeTrue();
        ((string)result).Should().Be("success");
    }

    [Fact]
    public async Task BlockMode_WithInvalidDPA_ReturnsError()
    {
        var processorId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var dpaService = Substitute.For<IDPAService>();
        dpaService.HasValidDPAAsync(processorId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, bool>>(Right<EncinaError, bool>(false)));
        dpaService.ValidateDPAAsync(processorId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, DPAValidationResult>>(
                Right<EncinaError, DPAValidationResult>(new DPAValidationResult
                {
                    ProcessorId = processorId.ToString(),
                    DPAId = null,
                    IsValid = false,
                    MissingTerms = [],
                    Warnings = [],
                    ValidatedAtUtc = DateTimeOffset.UtcNow
                })));

        var options = Options.Create(new ProcessorAgreementOptions
        {
            EnforcementMode = ProcessorAgreementEnforcementMode.Block
        });
        var logger = NullLogger<ProcessorValidationPipelineBehavior<TestCommand, string>>.Instance;

        var behavior = new ProcessorValidationPipelineBehavior<TestCommand, string>(
            dpaService, options, logger);

        var request = new TestCommand("test");
        var context = Substitute.For<IRequestContext>();
        var nextCalled = false;
        RequestHandlerCallback<string> next = () =>
        {
            nextCalled = true;
            return new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("should not reach"));
        };

        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        result.IsLeft.Should().BeTrue();
        nextCalled.Should().BeFalse();
    }

    #endregion
}
