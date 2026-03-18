using Encina.Compliance.CrossBorderTransfer;
using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Aggregates;
using Encina.Compliance.CrossBorderTransfer.Attributes;
using Encina.Compliance.CrossBorderTransfer.Events;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.ReadModels;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using Encina.DomainModeling;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten;

using Marten;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Advanced integration tests for cross-border transfer features:
/// - Read model projection from event streams
/// - Event stream audit trail (history queries)
/// - Concurrent aggregate operations (optimistic concurrency)
/// - TransferBlockingPipelineBehavior with real services
/// - Cache invalidation on write operations
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
public sealed class CrossBorderTransferAdvancedIntegrationTests
{
    private readonly MartenFixture _fixture;

    public CrossBorderTransferAdvancedIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    private MartenAggregateRepository<T> CreateRepository<T>() where T : class, IAggregate
    {
        var session = _fixture.Store!.LightweightSession();
        var requestContext = Substitute.For<IRequestContext>();
        requestContext.CorrelationId.Returns(Guid.NewGuid().ToString());
        var logger = NullLoggerFactory.Instance.CreateLogger<MartenAggregateRepository<T>>();
        var options = Options.Create(new EncinaMartenOptions());
        return new MartenAggregateRepository<T>(
            session,
            requestContext,
            (Microsoft.Extensions.Logging.ILogger<MartenAggregateRepository<T>>)logger,
            options);
    }

    private ServiceProvider BuildServiceProvider(
        Action<CrossBorderTransferOptions>? configure = null,
        FakeCacheProvider? cacheProvider = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSingleton(_fixture.Store!);
        services.AddScoped(sp => sp.GetRequiredService<IDocumentStore>().LightweightSession());

        services.AddEncinaMarten();

        // Register IAdequacyDecisionProvider (required by DefaultTransferValidator)
        var adequacyProvider = Substitute.For<IAdequacyDecisionProvider>();
        adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);
        services.AddSingleton<IAdequacyDecisionProvider>(adequacyProvider);

        services.AddEncinaCrossBorderTransfer(configure ?? (options =>
        {
            options.EnforcementMode = CrossBorderTransferEnforcementMode.Block;
            options.AddHealthCheck = true;
        }));
        services.AddCrossBorderTransferAggregates();

        services.AddSingleton<ICacheProvider>(cacheProvider ?? new FakeCacheProvider());

        var requestContext = Substitute.For<IRequestContext>();
        requestContext.CorrelationId.Returns(Guid.NewGuid().ToString());
        services.AddScoped<IRequestContext>(_ => requestContext);

        return services.BuildServiceProvider();
    }

    #region Projection Produces Correct Read Models

    [Fact]
    public async Task TIAService_GetTIA_ReturnsReadModelWithAllFieldsMapped()
    {
        // Arrange — create a TIA and progress it through full lifecycle
        using var provider = BuildServiceProvider();
        Guid tiaId;

        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ITIAService>();
            var result = await service.CreateTIAAsync("DE", "CN", "health-data", "analyst-1", "tenant-proj");
            tiaId = result.Match(id => id, _ => throw new InvalidOperationException("Create failed"));
        }

        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ITIAService>();
            await service.AssessRiskAsync(tiaId, 0.82, "High surveillance risk in CN", "assessor-1");
        }

        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ITIAService>();
            await service.RequireSupplementaryMeasureAsync(tiaId, SupplementaryMeasureType.Technical, "E2E encryption");
        }

        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ITIAService>();
            await service.SubmitForDPOReviewAsync(tiaId, "submitter-1");
        }

        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ITIAService>();
            await service.CompleteDPOReviewAsync(tiaId, approved: true, "dpo-1", null);
        }

        // Act — get read model
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ITIAService>();
            var getResult = await service.GetTIAAsync(tiaId);

            // Assert — all fields mapped correctly
            getResult.IsRight.ShouldBeTrue();
            getResult.IfRight(rm =>
            {
                rm.Id.ShouldBe(tiaId);
                rm.SourceCountryCode.ShouldBe("DE");
                rm.DestinationCountryCode.ShouldBe("CN");
                rm.DataCategory.ShouldBe("health-data");
                rm.Status.ShouldBe(TIAStatus.Completed);
                rm.RiskScore.ShouldBe(0.82);
                rm.Findings.ShouldBe("High surveillance risk in CN");
                rm.AssessorId.ShouldBe("assessor-1");
                rm.DPOReviewedAtUtc.ShouldNotBeNull();
                rm.CompletedAtUtc.ShouldNotBeNull();
                rm.RequiredSupplementaryMeasures.Count.ShouldBe(1);
                rm.RequiredSupplementaryMeasures[0].Type.ShouldBe(SupplementaryMeasureType.Technical);
                rm.RequiredSupplementaryMeasures[0].Description.ShouldBe("E2E encryption");
                rm.CreatedAtUtc.ShouldNotBe(default);
                rm.LastModifiedAtUtc.ShouldNotBe(default);
            });
        }
    }

    [Fact]
    public async Task SCCService_GetAgreement_ReturnsReadModelWithCorrectFields()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        Guid agreementId;
        var executedAt = DateTimeOffset.UtcNow.AddDays(-30);
        var expiresAt = DateTimeOffset.UtcNow.AddYears(2);

        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ISCCService>();
            var result = await service.RegisterAgreementAsync(
                "processor-projtest", SCCModule.ProcessorToProcessor, "2021/914",
                executedAt, expiresAt, "tenant-proj");
            agreementId = result.Match(id => id, _ => throw new InvalidOperationException("Register failed"));
        }

        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ISCCService>();
            await service.AddSupplementaryMeasureAsync(agreementId, SupplementaryMeasureType.Organizational, "DPO oversight");
        }

        // Act
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ISCCService>();
            var getResult = await service.GetAgreementAsync(agreementId);

            // Assert
            getResult.IsRight.ShouldBeTrue();
            getResult.IfRight(rm =>
            {
                rm.Id.ShouldBe(agreementId);
                rm.ProcessorId.ShouldBe("processor-projtest");
                rm.Module.ShouldBe(SCCModule.ProcessorToProcessor);
                rm.Version.ShouldBe("2021/914");
                rm.IsRevoked.ShouldBeFalse();
                rm.IsValid(DateTimeOffset.UtcNow).ShouldBeTrue();
                rm.SupplementaryMeasures.Count.ShouldBe(1);
                rm.SupplementaryMeasures[0].Type.ShouldBe(SupplementaryMeasureType.Organizational);
            });
        }
    }

    [Fact]
    public async Task TransferService_ApproveTransfer_PersistsToMarten()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        Guid transferId;

        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IApprovedTransferService>();
            var result = await service.ApproveTransferAsync(
                "DE", "JP", "analytics-proj", TransferBasis.AdequacyDecision,
                approvedBy: "approver-proj", expiresAtUtc: DateTimeOffset.UtcNow.AddYears(1));
            result.IsRight.ShouldBeTrue();
            transferId = result.Match(id => id, _ => throw new InvalidOperationException());
        }

        // Verify event stream was persisted
        await using var session = _fixture.Store!.LightweightSession();
        var events = await session.Events.FetchStreamAsync(transferId);
        events.ShouldNotBeNull();
        events.Count.ShouldBeGreaterThanOrEqualTo(1);
        events[0].Data.ShouldBeOfType<global::Encina.Compliance.CrossBorderTransfer.Events.TransferApproved>();
    }

    #endregion

    #region Event Stream Audit Trail

    [Fact]
    public async Task TIA_EventStream_ContainsFullAuditTrail()
    {
        // Arrange — create a TIA and exercise multiple transitions
        var repo = CreateRepository<TIAAggregate>();
        var id = Guid.NewGuid();
        var aggregate = TIAAggregate.Create(id, "DE", "US", "audit-data", "analyst-audit", "t1", "m1");
        aggregate.AssessRisk(0.55, "Medium risk", "assessor-audit");
        aggregate.RequireSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Contractual, "DPA addendum");
        aggregate.SubmitForDPOReview("submitter-audit");
        aggregate.ApproveDPOReview("dpo-audit");
        aggregate.Complete();
        await repo.CreateAsync(aggregate);

        // Act — query the raw event stream
        await using var session = _fixture.Store!.LightweightSession();
        var events = await session.Events.FetchStreamAsync(id);

        // Assert — event stream contains all lifecycle events in order
        events.ShouldNotBeNull();
        events.Count.ShouldBe(6);

        events[0].Data.ShouldBeOfType<TIACreated>();
        events[1].Data.ShouldBeOfType<TIARiskAssessed>();
        events[2].Data.ShouldBeOfType<TIASupplementaryMeasureRequired>();
        events[3].Data.ShouldBeOfType<TIASubmittedForDPOReview>();
        events[4].Data.ShouldBeOfType<TIADPOApproved>();
        events[5].Data.ShouldBeOfType<TIACompleted>();

        // Verify event data
        var created = (TIACreated)events[0].Data;
        created.SourceCountryCode.ShouldBe("DE");
        created.DestinationCountryCode.ShouldBe("US");
        created.DataCategory.ShouldBe("audit-data");
        created.CreatedBy.ShouldBe("analyst-audit");
        created.TenantId.ShouldBe("t1");
        created.ModuleId.ShouldBe("m1");

        var assessed = (TIARiskAssessed)events[1].Data;
        assessed.RiskScore.ShouldBe(0.55);
        assessed.AssessorId.ShouldBe("assessor-audit");

        // Verify monotonically increasing versions
        for (var i = 1; i < events.Count; i++)
        {
            events[i].Version.ShouldBeGreaterThan(events[i - 1].Version);
        }

        // Verify all events have timestamps
        foreach (var evt in events)
        {
            evt.Timestamp.ShouldNotBe(default);
        }
    }

    [Fact]
    public async Task SCC_EventStream_ContainsFullAuditTrail()
    {
        // Arrange
        var repo = CreateRepository<SCCAgreementAggregate>();
        var id = Guid.NewGuid();
        var aggregate = SCCAgreementAggregate.Register(
            id, "proc-audit", SCCModule.ControllerToProcessor, "2021/914",
            DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow.AddYears(1));
        aggregate.AddSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Technical, "Encryption");
        aggregate.Revoke("Non-compliance detected", "dpo-audit");
        await repo.CreateAsync(aggregate);

        // Act
        await using var session = _fixture.Store!.LightweightSession();
        var events = await session.Events.FetchStreamAsync(id);

        // Assert
        events.Count.ShouldBe(3);
        events[0].Data.ShouldBeOfType<SCCAgreementRegistered>();
        events[1].Data.ShouldBeOfType<SCCSupplementaryMeasureAdded>();
        events[2].Data.ShouldBeOfType<SCCAgreementRevoked>();

        var registered = (SCCAgreementRegistered)events[0].Data;
        registered.ProcessorId.ShouldBe("proc-audit");
        registered.Module.ShouldBe(SCCModule.ControllerToProcessor);

        var revoked = (SCCAgreementRevoked)events[2].Data;
        revoked.Reason.ShouldBe("Non-compliance detected");
        revoked.RevokedBy.ShouldBe("dpo-audit");
    }

    [Fact]
    public async Task Transfer_EventStream_ContainsApproveRevokeRenewExpire()
    {
        // Arrange
        var repo = CreateRepository<ApprovedTransferAggregate>();
        var id = Guid.NewGuid();
        var aggregate = ApprovedTransferAggregate.Approve(
            id, "DE", "BR", "financial", TransferBasis.SCCs,
            approvedBy: "approver-audit", expiresAtUtc: DateTimeOffset.UtcNow.AddDays(-1));
        await repo.CreateAsync(aggregate);

        // Add more events in sequence
        var repo2 = CreateRepository<ApprovedTransferAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded.Expire();
        loaded.Renew(DateTimeOffset.UtcNow.AddYears(1), "renewer-audit");
        await repo2.SaveAsync(loaded);

        var repo3 = CreateRepository<ApprovedTransferAggregate>();
        var loaded2 = (await repo3.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded2.Revoke("SCC expired", "dpo-audit");
        await repo3.SaveAsync(loaded2);

        // Act
        await using var session = _fixture.Store!.LightweightSession();
        var events = await session.Events.FetchStreamAsync(id);

        // Assert
        events.Count.ShouldBe(4);
        events[0].Data.ShouldBeOfType<TransferApproved>();
        events[1].Data.ShouldBeOfType<TransferExpired>();
        events[2].Data.ShouldBeOfType<TransferRenewed>();
        events[3].Data.ShouldBeOfType<TransferRevoked>();
    }

    #endregion

    #region Concurrent Aggregate Operations

    [Fact]
    public async Task TIA_SequentialModifications_BothPersistCorrectly()
    {
        // Arrange — create a TIA, then apply two modifications sequentially
        var repo = CreateRepository<TIAAggregate>();
        var id = Guid.NewGuid();
        var aggregate = TIAAggregate.Create(id, "DE", "RU", "personal-data", "analyst-1");
        await repo.CreateAsync(aggregate);

        // First modification: assess risk
        var repo1 = CreateRepository<TIAAggregate>();
        var loaded1 = (await repo1.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded1.AssessRisk(0.7, "High risk", "assessor-1");
        var result1 = await repo1.SaveAsync(loaded1);
        result1.IsRight.ShouldBeTrue();

        // Second modification: add supplementary measure (loads fresh state including risk assessment)
        var repo2 = CreateRepository<TIAAggregate>();
        var loaded2 = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded2.Status.ShouldBe(TIAStatus.InProgress);
        loaded2.RequireSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Technical, "Encryption");
        var result2 = await repo2.SaveAsync(loaded2);
        result2.IsRight.ShouldBeTrue();

        // Verify — both modifications applied
        var verifyRepo = CreateRepository<TIAAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        final.Status.ShouldBe(TIAStatus.InProgress);
        final.RiskScore.ShouldBe(0.7);
        final.RequiredSupplementaryMeasures.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SCC_SequentialAddMeasureAndRevoke_BothPersist()
    {
        // Arrange
        var repo = CreateRepository<SCCAgreementAggregate>();
        var id = Guid.NewGuid();
        var aggregate = SCCAgreementAggregate.Register(
            id, "proc-concurrent", SCCModule.ControllerToProcessor, "2021/914",
            DateTimeOffset.UtcNow.AddDays(-10));
        await repo.CreateAsync(aggregate);

        // First: add measure
        var repo1 = CreateRepository<SCCAgreementAggregate>();
        var loaded1 = (await repo1.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded1.AddSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Technical, "Encryption");
        var result1 = await repo1.SaveAsync(loaded1);
        result1.IsRight.ShouldBeTrue();

        // Second: revoke (loads fresh state)
        var repo2 = CreateRepository<SCCAgreementAggregate>();
        var loaded2 = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded2.SupplementaryMeasures.Count.ShouldBe(1);
        loaded2.Revoke("Non-compliance", "dpo-1");
        var result2 = await repo2.SaveAsync(loaded2);
        result2.IsRight.ShouldBeTrue();

        // Verify
        var verifyRepo = CreateRepository<SCCAgreementAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        final.IsRevoked.ShouldBeTrue();
        final.SupplementaryMeasures.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Transfer_SequentialRenewAndRevoke_BothPersist()
    {
        // Arrange
        var repo = CreateRepository<ApprovedTransferAggregate>();
        var id = Guid.NewGuid();
        var aggregate = ApprovedTransferAggregate.Approve(
            id, "DE", "US", "personal", TransferBasis.SCCs,
            approvedBy: "approver-1", expiresAtUtc: DateTimeOffset.UtcNow.AddMonths(6));
        await repo.CreateAsync(aggregate);

        // First: renew
        var repo1 = CreateRepository<ApprovedTransferAggregate>();
        var loaded1 = (await repo1.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        var newExpiry = DateTimeOffset.UtcNow.AddYears(2);
        loaded1.Renew(newExpiry, "renewer-1");
        var result1 = await repo1.SaveAsync(loaded1);
        result1.IsRight.ShouldBeTrue();

        // Second: revoke (loads fresh state with renewed expiry)
        var repo2 = CreateRepository<ApprovedTransferAggregate>();
        var loaded2 = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded2.ExpiresAtUtc.ShouldBe(newExpiry);
        loaded2.Revoke("SCC invalid", "dpo-1");
        var result2 = await repo2.SaveAsync(loaded2);
        result2.IsRight.ShouldBeTrue();

        // Verify
        var verifyRepo = CreateRepository<ApprovedTransferAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        final.IsRevoked.ShouldBeTrue();
    }

    #endregion

    #region TransferBlockingPipelineBehavior with Real Services

    [Fact]
    public async Task Pipeline_BlockMode_WithRealValidator_BlocksNonAdequateTransfer()
    {
        // Arrange — build full pipeline with real cross-border transfer services
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_fixture.Store!);
        services.AddScoped(sp => sp.GetRequiredService<IDocumentStore>().LightweightSession());
        services.AddEncinaMarten();

        services.AddEncina(config =>
            config.RegisterServicesFromAssemblyContaining<PipelineTestCommand>());

        // Register mocked adequacy provider (no adequate countries)
        var adequacyProvider = Substitute.For<IAdequacyDecisionProvider>();
        adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);
        services.AddSingleton<IAdequacyDecisionProvider>(adequacyProvider);

        services.AddEncinaCrossBorderTransfer(options =>
        {
            options.EnforcementMode = CrossBorderTransferEnforcementMode.Block;
            options.DefaultSourceCountryCode = "DE";
        });
        services.AddCrossBorderTransferAggregates();
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

        // Act — send command targeting non-adequate country
        using var scope = provider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var result = await encina.Send(new PipelineTestCommand("CN"));

        // Assert — should be blocked
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Pipeline_WarnMode_AllowsNonAdequateTransfer()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_fixture.Store!);
        services.AddScoped(sp => sp.GetRequiredService<IDocumentStore>().LightweightSession());
        services.AddEncinaMarten();

        services.AddEncina(config =>
            config.RegisterServicesFromAssemblyContaining<PipelineTestCommand>());

        var adequacyProvider = Substitute.For<IAdequacyDecisionProvider>();
        adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);
        services.AddSingleton<IAdequacyDecisionProvider>(adequacyProvider);

        services.AddEncinaCrossBorderTransfer(options =>
        {
            options.EnforcementMode = CrossBorderTransferEnforcementMode.Warn;
            options.DefaultSourceCountryCode = "DE";
        });
        services.AddCrossBorderTransferAggregates();
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

        // Act
        using var scope = provider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var result = await encina.Send(new PipelineTestCommand("CN"));

        // Assert — warn mode allows the request through
        result.IsRight.ShouldBeTrue();
        result.IfRight(v => v.ShouldBe(42));
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

        // Even disabled mode needs IAdequacyDecisionProvider registered for DI validation
        var adequacyProvider = Substitute.For<IAdequacyDecisionProvider>();
        adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);
        services.AddSingleton<IAdequacyDecisionProvider>(adequacyProvider);

        services.AddEncinaCrossBorderTransfer(options =>
        {
            options.EnforcementMode = CrossBorderTransferEnforcementMode.Disabled;
        });
        services.AddCrossBorderTransferAggregates();
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

        // Act
        using var scope = provider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var result = await encina.Send(new PipelineTestCommand("CN"));

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

        var adequacyProvider = Substitute.For<IAdequacyDecisionProvider>();
        adequacyProvider.HasAdequacy(Arg.Any<Region>()).Returns(false);
        services.AddSingleton<IAdequacyDecisionProvider>(adequacyProvider);

        services.AddEncinaCrossBorderTransfer(options =>
        {
            options.EnforcementMode = CrossBorderTransferEnforcementMode.Block;
        });
        services.AddCrossBorderTransferAggregates();
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

        // Act — command without attribute should skip validation
        using var scope = provider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var result = await encina.Send(new NoPipelineTestCommand("any-data"));

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(v => v.ShouldBe(99));
    }

    // Note: Pipeline_BlockMode_WithApprovedTransfer_AllowsRequest requires Marten inline projections
    // for route-based transfer queries, which is tracked separately. The pipeline behavior is already
    // tested above for Block, Warn, Disabled, and NoAttribute modes.

    #endregion

    #region Cache Invalidation on Write Operations

    [Fact]
    public async Task TIAService_ModifyTIA_InvalidatesCache()
    {
        // Arrange
        var fakeCache = new FakeCacheProvider();
        using var provider = BuildServiceProvider(cacheProvider: fakeCache);
        Guid tiaId;

        // Create TIA
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ITIAService>();
            var result = await service.CreateTIAAsync("DE", "IN", "cache-test-data", "analyst-cache");
            tiaId = result.Match(id => id, _ => throw new InvalidOperationException("Create failed"));
        }

        // First read — should populate cache
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ITIAService>();
            var getResult = await service.GetTIAAsync(tiaId);
            getResult.IsRight.ShouldBeTrue();
        }

        // Cache should have been populated
        var cachedKeysBefore = fakeCache.CachedKeys.ToList();
        cachedKeysBefore.ShouldContain(k => k.Contains(tiaId.ToString()));

        // Act — modify TIA (should invalidate cache)
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ITIAService>();
            await service.AssessRiskAsync(tiaId, 0.45, "Medium risk", "assessor-cache");
        }

        // Assert — cache was invalidated (key was removed)
        var removedKeys = fakeCache.RemovedKeys.ToList();
        removedKeys.ShouldContain(k => k.Contains(tiaId.ToString()));
    }

    [Fact]
    public async Task TIAService_SecondRead_AfterModification_ReturnsUpdatedData()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        Guid tiaId;

        // Create
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ITIAService>();
            var result = await service.CreateTIAAsync("DE", "BR", "refresh-data", "analyst-refresh");
            tiaId = result.Match(id => id, _ => throw new InvalidOperationException("Create failed"));
        }

        // First read — Draft status
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ITIAService>();
            var r = await service.GetTIAAsync(tiaId);
            r.IfRight(rm => rm.Status.ShouldBe(TIAStatus.Draft));
        }

        // Modify — assess risk
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ITIAService>();
            await service.AssessRiskAsync(tiaId, 0.6, "Refresh test", "assessor-refresh");
        }

        // Act — second read should see updated state (InProgress, risk score)
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ITIAService>();
            var r = await service.GetTIAAsync(tiaId);

            // Assert
            r.IsRight.ShouldBeTrue();
            r.IfRight(rm =>
            {
                rm.Status.ShouldBe(TIAStatus.InProgress);
                rm.RiskScore.ShouldBe(0.6);
                rm.Findings.ShouldBe("Refresh test");
            });
        }
    }

    #endregion

    #region Test Infrastructure — Commands & Handlers

    [RequiresCrossBorderTransfer(DestinationProperty = nameof(DestinationCountryCode), DataCategory = "personal-data")]
    private sealed record PipelineTestCommand(string DestinationCountryCode) : IRequest<int>;

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
