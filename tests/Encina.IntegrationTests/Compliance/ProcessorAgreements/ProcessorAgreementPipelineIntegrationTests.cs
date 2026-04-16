#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.Scheduling;
using Encina.Compliance.ProcessorAgreements.Services;

using Shouldly;

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
        descriptor.ShouldNotBeNull();
        descriptor!.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        descriptor.ImplementationType.ShouldBe(typeof(DefaultProcessorService));
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersIDPAService_AsScoped()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IDPAService));
        descriptor.ShouldNotBeNull();
        descriptor!.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        descriptor.ImplementationType.ShouldBe(typeof(DefaultDPAService));
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersPipelineBehavior_AsTransient()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements();

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>));
        descriptor.ShouldNotBeNull();
        descriptor!.Lifetime.ShouldBe(ServiceLifetime.Transient);
        descriptor.ImplementationType.ShouldBe(typeof(ProcessorValidationPipelineBehavior<,>));
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersExpirationHandler_AsTransient()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements();

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ICommandHandler<CheckDPAExpirationCommand, Unit>));
        descriptor.ShouldNotBeNull();
        descriptor!.Lifetime.ShouldBe(ServiceLifetime.Transient);
        descriptor.ImplementationType.ShouldBe(typeof(CheckDPAExpirationHandler));
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersTimeProvider_AsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TimeProvider));
        descriptor.ShouldNotBeNull();
        descriptor!.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersOptions_Resolvable()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements();
        var provider = services.BuildServiceProvider();

        var options = provider.GetService<IOptions<ProcessorAgreementOptions>>();
        options.ShouldNotBeNull();
        options!.Value.ShouldNotBeNull();
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

        options.EnforcementMode.ShouldBe(ProcessorAgreementEnforcementMode.Warn);
        options.BlockWithoutValidDPA.ShouldBeFalse();
        options.MaxSubProcessorDepth.ShouldBe(3);
        options.EnableExpirationMonitoring.ShouldBeFalse();
        options.ExpirationCheckInterval.ShouldBe(TimeSpan.FromHours(1));
        options.ExpirationWarningDays.ShouldBe(30);
        options.TrackAuditTrail.ShouldBeTrue();
        options.AddHealthCheck.ShouldBeFalse();
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

        options.EnforcementMode.ShouldBe(ProcessorAgreementEnforcementMode.Block);
        options.MaxSubProcessorDepth.ShouldBe(5);
        options.ExpirationWarningDays.ShouldBe(60);
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

        options.EnforcementMode.ShouldBe(ProcessorAgreementEnforcementMode.Block);
        options.BlockWithoutValidDPA.ShouldBeTrue();
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
        healthCheckDescriptor.ShouldBeNull();
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
        healthCheckDescriptor.ShouldNotBeNull();
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
        descriptor.ImplementationType.ShouldNotBe(typeof(DefaultDPAService));
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
        descriptor.ImplementationType.ShouldNotBe(typeof(DefaultProcessorService));
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

        result.IsRight.ShouldBeTrue();
        ((string)result).ShouldBe("success");
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

        result.IsRight.ShouldBeTrue();
        ((string)result).ShouldBe("success");
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

        result.IsLeft.ShouldBeTrue();
        nextCalled.ShouldBeFalse();
    }

    #endregion
}
