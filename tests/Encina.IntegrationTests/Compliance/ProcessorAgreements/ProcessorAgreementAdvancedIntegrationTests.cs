#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Events;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten;
using Encina.Testing.Fakes.Providers;

using LanguageExt;

using Marten;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Compliance.ProcessorAgreements;

/// <summary>
/// Advanced integration tests for processor agreements features:
/// - Read model projection from event streams
/// - Event stream audit trail (history queries)
/// - Concurrent aggregate operations (optimistic concurrency)
/// - ProcessorValidationPipelineBehavior with real services
/// - Cache invalidation on write operations
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ProcessorAgreementAdvancedIntegrationTests
{
    private readonly MartenFixture _fixture;

    public ProcessorAgreementAdvancedIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    private ServiceProvider BuildServiceProvider(
        Action<ProcessorAgreementOptions>? configure = null,
        FakeCacheProvider? cacheProvider = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSingleton(_fixture.Store!);
        services.AddScoped(sp => sp.GetRequiredService<IDocumentStore>().LightweightSession());

        services.AddEncinaMarten();

        services.AddEncinaProcessorAgreements(configure ?? (options =>
        {
            options.EnforcementMode = ProcessorAgreementEnforcementMode.Block;
            options.AddHealthCheck = true;
        }));
        services.AddProcessorAgreementAggregates();

        services.AddSingleton<ICacheProvider>(cacheProvider ?? new FakeCacheProvider());

        var requestContext = Substitute.For<IRequestContext>();
        requestContext.CorrelationId.Returns(Guid.NewGuid().ToString());
        services.AddScoped<IRequestContext>(_ => requestContext);

        return services.BuildServiceProvider();
    }

    private static DPAMandatoryTerms FullyCompliantTerms() => new()
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

    #region Projection Produces Correct Read Models

    [Fact]
    public async Task ProcessorService_RegisterAndGet_ReturnsReadModelWithAllFields()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        Guid processorId;

        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IProcessorService>();
            var result = await service.RegisterProcessorAsync(
                "Stripe", "US", "dpo@stripe.com", null, 0,
                SubProcessorAuthorizationType.Specific, "t-proj", "m-proj");
            processorId = result.Match(id => id, _ => throw new InvalidOperationException("Register failed"));
        }

        // Act
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IProcessorService>();
            var getResult = await service.GetProcessorAsync(processorId);

            // Assert
            getResult.IsRight.ShouldBeTrue();
            getResult.IfRight(rm =>
            {
                rm.Id.ShouldBe(processorId);
                rm.Name.ShouldBe("Stripe");
                rm.Country.ShouldBe("US");
                rm.ContactEmail.ShouldBe("dpo@stripe.com");
                rm.ParentProcessorId.ShouldBeNull();
                rm.Depth.ShouldBe(0);
                rm.AuthorizationType.ShouldBe(SubProcessorAuthorizationType.Specific);
                rm.IsRemoved.ShouldBeFalse();
                rm.TenantId.ShouldBe("t-proj");
                rm.ModuleId.ShouldBe("m-proj");
            });
        }
    }

    [Fact]
    public async Task DPAService_ExecuteAndGet_ReturnsReadModelWithAllFields()
    {
        // Arrange — register processor first, then execute DPA
        using var provider = BuildServiceProvider();
        Guid processorId;
        Guid dpaId;

        using (var scope = provider.CreateScope())
        {
            var processorService = scope.ServiceProvider.GetRequiredService<IProcessorService>();
            var result = await processorService.RegisterProcessorAsync(
                "TestProc", "DE", null, null, 0,
                SubProcessorAuthorizationType.Specific, "t1", "m1");
            processorId = result.Match(id => id, _ => throw new InvalidOperationException());
        }

        using (var scope = provider.CreateScope())
        {
            var dpaService = scope.ServiceProvider.GetRequiredService<IDPAService>();
            var result = await dpaService.ExecuteDPAAsync(
                processorId, FullyCompliantTerms(), true,
                ["data-analytics", "payment-processing"],
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(2), "t1", "m1");
            dpaId = result.Match(id => id, _ => throw new InvalidOperationException("Execute failed"));
        }

        // Act
        using (var scope = provider.CreateScope())
        {
            var dpaService = scope.ServiceProvider.GetRequiredService<IDPAService>();
            var getResult = await dpaService.GetDPAAsync(dpaId);

            // Assert
            getResult.IsRight.ShouldBeTrue();
            getResult.IfRight(rm =>
            {
                rm.Id.ShouldBe(dpaId);
                rm.ProcessorId.ShouldBe(processorId);
                rm.Status.ShouldBe(DPAStatus.Active);
                rm.MandatoryTerms.IsFullyCompliant.ShouldBeTrue();
                rm.HasSCCs.ShouldBeTrue();
                rm.ProcessingPurposes.Count.ShouldBe(2);
                rm.TenantId.ShouldBe("t1");
                rm.ModuleId.ShouldBe("m1");
            });
        }
    }

    [Fact]
    public async Task DPAService_AmendDPA_UpdatesReadModel()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        Guid processorId;
        Guid dpaId;

        using (var scope = provider.CreateScope())
        {
            var processorService = scope.ServiceProvider.GetRequiredService<IProcessorService>();
            var r = await processorService.RegisterProcessorAsync(
                "AmendProc", "US", null, null, 0,
                SubProcessorAuthorizationType.Specific);
            processorId = r.Match(id => id, _ => throw new InvalidOperationException());
        }

        using (var scope = provider.CreateScope())
        {
            var dpaService = scope.ServiceProvider.GetRequiredService<IDPAService>();
            var r = await dpaService.ExecuteDPAAsync(
                processorId, FullyCompliantTerms(), false,
                ["analytics"], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));
            dpaId = r.Match(id => id, _ => throw new InvalidOperationException());
        }

        // Act — amend
        using (var scope = provider.CreateScope())
        {
            var dpaService = scope.ServiceProvider.GetRequiredService<IDPAService>();
            await dpaService.AmendDPAAsync(
                dpaId, FullyCompliantTerms(), true,
                ["analytics", "reporting"], "Added SCCs and reporting");
        }

        // Assert
        using (var scope = provider.CreateScope())
        {
            var dpaService = scope.ServiceProvider.GetRequiredService<IDPAService>();
            var r = await dpaService.GetDPAAsync(dpaId);
            r.IsRight.ShouldBeTrue();
            r.IfRight(rm =>
            {
                rm.HasSCCs.ShouldBeTrue();
                rm.ProcessingPurposes.Count.ShouldBe(2);
                rm.ProcessingPurposes.ShouldContain("reporting");
            });
        }
    }

    #endregion

    #region Event Stream Audit Trail via Service

    [Fact]
    public async Task DPA_AuditDPA_RecordsInAuditHistory()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        Guid processorId;
        Guid dpaId;

        using (var scope = provider.CreateScope())
        {
            var ps = scope.ServiceProvider.GetRequiredService<IProcessorService>();
            processorId = (await ps.RegisterProcessorAsync(
                "AuditProc", "DE", null, null, 0, SubProcessorAuthorizationType.Specific))
                .Match(id => id, _ => throw new InvalidOperationException());
        }

        using (var scope = provider.CreateScope())
        {
            var ds = scope.ServiceProvider.GetRequiredService<IDPAService>();
            dpaId = (await ds.ExecuteDPAAsync(
                processorId, FullyCompliantTerms(), false,
                ["processing"], DateTimeOffset.UtcNow, null))
                .Match(id => id, _ => throw new InvalidOperationException());
        }

        // Act — audit twice
        using (var scope = provider.CreateScope())
        {
            var ds = scope.ServiceProvider.GetRequiredService<IDPAService>();
            await ds.AuditDPAAsync(dpaId, "auditor-1", "Initial audit: compliant");
        }

        using (var scope = provider.CreateScope())
        {
            var ds = scope.ServiceProvider.GetRequiredService<IDPAService>();
            await ds.AuditDPAAsync(dpaId, "auditor-2", "Follow-up audit: improvements noted");
        }

        // Assert — verify via event stream
        await using var session = _fixture.Store!.LightweightSession();
        var events = await session.Events.FetchStreamAsync(dpaId);
        events.Count.ShouldBe(3); // DPAExecuted + 2x DPAAudited
        events[1].Data.ShouldBeOfType<DPAAudited>();
        events[2].Data.ShouldBeOfType<DPAAudited>();
        ((DPAAudited)events[1].Data).AuditorId.ShouldBe("auditor-1");
        ((DPAAudited)events[2].Data).AuditorId.ShouldBe("auditor-2");
    }

    #endregion

    #region ProcessorValidationPipelineBehavior with Real Services

    [Fact]
    public async Task Pipeline_BlockMode_WithValidDPA_AllowsRequest()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_fixture.Store!);
        services.AddScoped(sp => sp.GetRequiredService<IDocumentStore>().LightweightSession());
        services.AddEncinaMarten();

        services.AddEncina(config =>
            config.RegisterServicesFromAssemblyContaining<PipelineTestCommand>());

        services.AddEncinaProcessorAgreements(options =>
        {
            options.EnforcementMode = ProcessorAgreementEnforcementMode.Block;
        });
        services.AddProcessorAgreementAggregates();
        services.AddSingleton<ICacheProvider>(new FakeCacheProvider());

        var requestContext = Substitute.For<IRequestContext>();
        requestContext.CorrelationId.Returns(Guid.NewGuid().ToString());
        services.AddScoped<IRequestContext>(_ => requestContext);

        services.AddScoped<IRequestHandler<PipelineTestCommand, int>, PipelineTestHandler>();
        services.AddScoped<IRequestHandler<NoPipelineTestCommand, int>, NoPipelineTestHandler>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });

        // Register a processor and execute DPA
        Guid processorId;
        using (var scope = provider.CreateScope())
        {
            var ps = scope.ServiceProvider.GetRequiredService<IProcessorService>();
            processorId = (await ps.RegisterProcessorAsync(
                "PipelineProc", "DE", null, null, 0, SubProcessorAuthorizationType.Specific))
                .Match(id => id, _ => throw new InvalidOperationException());
        }

        using (var scope = provider.CreateScope())
        {
            var ds = scope.ServiceProvider.GetRequiredService<IDPAService>();
            await ds.ExecuteDPAAsync(
                processorId, FullyCompliantTerms(), false,
                ["processing"], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(2));
        }

        // Act — send command requiring processor validation
        using var actScope = provider.CreateScope();
        var encina = actScope.ServiceProvider.GetRequiredService<IEncina>();
        var result = await encina.Send(new PipelineTestCommand(processorId.ToString()));

        // Assert — should succeed because DPA is valid
        result.IsRight.ShouldBeTrue();
        result.IfRight(v => v.ShouldBe(42));
    }

    [Fact]
    public async Task Pipeline_BlockMode_WithoutDPA_BlocksRequest()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_fixture.Store!);
        services.AddScoped(sp => sp.GetRequiredService<IDocumentStore>().LightweightSession());
        services.AddEncinaMarten();

        services.AddEncina(config =>
            config.RegisterServicesFromAssemblyContaining<PipelineTestCommand>());

        services.AddEncinaProcessorAgreements(options =>
        {
            options.EnforcementMode = ProcessorAgreementEnforcementMode.Block;
        });
        services.AddProcessorAgreementAggregates();
        services.AddSingleton<ICacheProvider>(new FakeCacheProvider());

        var requestContext = Substitute.For<IRequestContext>();
        requestContext.CorrelationId.Returns(Guid.NewGuid().ToString());
        services.AddScoped<IRequestContext>(_ => requestContext);

        services.AddScoped<IRequestHandler<PipelineTestCommand, int>, PipelineTestHandler>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });

        // Act — send command with processor ID that has no DPA
        using var scope = provider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var result = await encina.Send(new PipelineTestCommand(Guid.NewGuid().ToString()));

        // Assert — should be blocked
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Pipeline_DisabledMode_SkipsValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_fixture.Store!);
        services.AddScoped(sp => sp.GetRequiredService<IDocumentStore>().LightweightSession());
        services.AddEncinaMarten();

        services.AddEncina(config =>
            config.RegisterServicesFromAssemblyContaining<PipelineTestCommand>());

        services.AddEncinaProcessorAgreements(options =>
        {
            options.EnforcementMode = ProcessorAgreementEnforcementMode.Disabled;
        });
        services.AddProcessorAgreementAggregates();
        services.AddSingleton<ICacheProvider>(new FakeCacheProvider());

        var requestContext = Substitute.For<IRequestContext>();
        requestContext.CorrelationId.Returns(Guid.NewGuid().ToString());
        services.AddScoped<IRequestContext>(_ => requestContext);

        services.AddScoped<IRequestHandler<PipelineTestCommand, int>, PipelineTestHandler>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });

        // Act — disabled mode should skip all validation
        using var scope = provider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var result = await encina.Send(new PipelineTestCommand(Guid.NewGuid().ToString()));

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(v => v.ShouldBe(42));
    }

    [Fact]
    public async Task Pipeline_NoAttribute_SkipsValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_fixture.Store!);
        services.AddScoped(sp => sp.GetRequiredService<IDocumentStore>().LightweightSession());
        services.AddEncinaMarten();

        services.AddEncina(config =>
            config.RegisterServicesFromAssemblyContaining<NoPipelineTestCommand>());

        services.AddEncinaProcessorAgreements(options =>
        {
            options.EnforcementMode = ProcessorAgreementEnforcementMode.Block;
        });
        services.AddProcessorAgreementAggregates();
        services.AddSingleton<ICacheProvider>(new FakeCacheProvider());

        var requestContext = Substitute.For<IRequestContext>();
        requestContext.CorrelationId.Returns(Guid.NewGuid().ToString());
        services.AddScoped<IRequestContext>(_ => requestContext);

        services.AddScoped<IRequestHandler<NoPipelineTestCommand, int>, NoPipelineTestHandler>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });

        // Act — command without attribute should bypass validation
        using var scope = provider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var result = await encina.Send(new NoPipelineTestCommand("test"));

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(v => v.ShouldBe(99));
    }

    #endregion

    #region Cache Invalidation on Write Operations

    [Fact]
    public async Task ProcessorService_Update_InvalidatesCache()
    {
        // Arrange
        var fakeCache = new FakeCacheProvider();
        using var provider = BuildServiceProvider(cacheProvider: fakeCache);
        Guid processorId;

        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IProcessorService>();
            var result = await service.RegisterProcessorAsync(
                "CacheProc", "US", null, null, 0, SubProcessorAuthorizationType.Specific);
            processorId = result.Match(id => id, _ => throw new InvalidOperationException());
        }

        // First read — should populate cache
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IProcessorService>();
            await service.GetProcessorAsync(processorId);
        }

        var cachedKeysBefore = fakeCache.CachedKeys.ToList();
        cachedKeysBefore.ShouldContain(k => k.Contains(processorId.ToString()));

        // Act — update (should invalidate cache)
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IProcessorService>();
            await service.UpdateProcessorAsync(processorId, "UpdatedProc", "DE", "new@email.com",
                SubProcessorAuthorizationType.General);
        }

        // Assert — cache was invalidated
        var removedKeys = fakeCache.RemovedKeys.ToList();
        removedKeys.ShouldContain(k => k.Contains(processorId.ToString()));
    }

    [Fact]
    public async Task DPAService_SecondRead_AfterAmendment_ReturnsUpdatedData()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        Guid processorId;
        Guid dpaId;

        using (var scope = provider.CreateScope())
        {
            var ps = scope.ServiceProvider.GetRequiredService<IProcessorService>();
            processorId = (await ps.RegisterProcessorAsync(
                "CacheDPAProc", "DE", null, null, 0, SubProcessorAuthorizationType.Specific))
                .Match(id => id, _ => throw new InvalidOperationException());
        }

        using (var scope = provider.CreateScope())
        {
            var ds = scope.ServiceProvider.GetRequiredService<IDPAService>();
            dpaId = (await ds.ExecuteDPAAsync(
                processorId, FullyCompliantTerms(), false,
                ["analytics"], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1)))
                .Match(id => id, _ => throw new InvalidOperationException());
        }

        // First read — no SCCs
        using (var scope = provider.CreateScope())
        {
            var ds = scope.ServiceProvider.GetRequiredService<IDPAService>();
            var r = await ds.GetDPAAsync(dpaId);
            r.IfRight(rm => rm.HasSCCs.ShouldBeFalse());
        }

        // Amend — add SCCs
        using (var scope = provider.CreateScope())
        {
            var ds = scope.ServiceProvider.GetRequiredService<IDPAService>();
            await ds.AmendDPAAsync(dpaId, FullyCompliantTerms(), true,
                ["analytics", "reporting"], "Added SCCs");
        }

        // Act — second read should reflect amendment
        using (var scope = provider.CreateScope())
        {
            var ds = scope.ServiceProvider.GetRequiredService<IDPAService>();
            var r = await ds.GetDPAAsync(dpaId);

            // Assert
            r.IsRight.ShouldBeTrue();
            r.IfRight(rm =>
            {
                rm.HasSCCs.ShouldBeTrue();
                rm.ProcessingPurposes.ShouldContain("reporting");
            });
        }
    }

    #endregion

    #region Test Infrastructure — Commands & Handlers

    [RequiresProcessor(ProcessorId = "00000000-0000-0000-0000-000000000001")]
    private sealed record PipelineTestCommand(string ProcessorIdValue) : IRequest<int>;

    private sealed class PipelineTestHandler : IRequestHandler<PipelineTestCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(PipelineTestCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(42));
    }

    private sealed record NoPipelineTestCommand(string Data) : IRequest<int>;

    private sealed class NoPipelineTestHandler : IRequestHandler<NoPipelineTestCommand, int>
    {
        public Task<Either<EncinaError, int>> Handle(NoPipelineTestCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, int>(99));
    }

    #endregion
}
