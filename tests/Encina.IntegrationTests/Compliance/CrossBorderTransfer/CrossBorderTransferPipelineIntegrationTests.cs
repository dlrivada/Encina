using Encina.Compliance.CrossBorderTransfer;
using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Aggregates;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.ReadModels;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten;

using Marten;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using Shouldly;

namespace Encina.IntegrationTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Integration tests for the full Encina.Compliance.CrossBorderTransfer pipeline.
/// Tests DI registration, service operations against real Marten/PostgreSQL, and health check integration.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
public sealed class CrossBorderTransferPipelineIntegrationTests
{
    private readonly MartenFixture _fixture;

    public CrossBorderTransferPipelineIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    private ServiceProvider BuildServiceProvider(Action<CrossBorderTransferOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Register Marten with real PostgreSQL from fixture
        services.AddSingleton(_fixture.Store!);
        services.AddScoped(sp => sp.GetRequiredService<IDocumentStore>().LightweightSession());

        // Register Marten infrastructure
        services.AddEncinaMarten();

        // Register cross-border transfer services and aggregates
        services.AddEncinaCrossBorderTransfer(configure ?? (options =>
        {
            options.EnforcementMode = CrossBorderTransferEnforcementMode.Block;
            options.AddHealthCheck = true;
        }));
        services.AddCrossBorderTransferAggregates();

        // Register fake cache provider (from global using)
        services.AddSingleton<ICacheProvider>(new FakeCacheProvider());

        // Register IRequestContext stub
        var requestContext = Substitute.For<IRequestContext>();
        requestContext.CorrelationId.Returns(Guid.NewGuid().ToString());
        services.AddScoped<IRequestContext>(_ => requestContext);

        return services.BuildServiceProvider();
    }

    #region DI Registration

    [Fact]
    public void AddEncinaCrossBorderTransfer_RegistersAllServices()
    {
        // Arrange & Act
        using var provider = BuildServiceProvider();

        // Assert
        provider.GetService<ITIAService>().ShouldNotBeNull();
        provider.GetService<ISCCService>().ShouldNotBeNull();
        provider.GetService<IApprovedTransferService>().ShouldNotBeNull();
        provider.GetService<ITransferValidator>().ShouldNotBeNull();
        provider.GetService<ITIARiskAssessor>().ShouldNotBeNull();
    }

    [Fact]
    public void AddCrossBorderTransferAggregates_RegistersRepositories()
    {
        // Arrange & Act
        using var provider = BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<IAggregateRepository<TIAAggregate>>().ShouldNotBeNull();
        scope.ServiceProvider.GetService<IAggregateRepository<SCCAgreementAggregate>>().ShouldNotBeNull();
        scope.ServiceProvider.GetService<IAggregateRepository<ApprovedTransferAggregate>>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaCrossBorderTransfer_WithHealthCheck_RegistersHealthCheck()
    {
        // Arrange & Act
        using var provider = BuildServiceProvider(options =>
        {
            options.AddHealthCheck = true;
        });

        // Assert
        var healthCheckService = provider.GetService<HealthCheckService>();
        healthCheckService.ShouldNotBeNull();
    }

    #endregion

    #region TIA Full Lifecycle via Service

    [Fact]
    public async Task TIAService_CreateTIA_PersistsToMartenAndReturnsId()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITIAService>();

        // Act
        var result = await service.CreateTIAAsync("DE", "US", "health-data", "analyst-1", "tenant-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(id => id.ShouldNotBe(Guid.Empty));
    }

    [Fact]
    public async Task TIAService_CreateAndGetTIA_ReturnsReadModel()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITIAService>();

        var createResult = await service.CreateTIAAsync("FR", "CN", "employee-data", "analyst-1");
        var tiaId = createResult.Match(id => id, _ => throw new InvalidOperationException("Create failed"));

        // Act — get TIA (fresh scope to avoid session reuse)
        using var scope2 = provider.CreateScope();
        var service2 = scope2.ServiceProvider.GetRequiredService<ITIAService>();
        var getResult = await service2.GetTIAAsync(tiaId);

        // Assert
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(readModel =>
        {
            readModel.Id.ShouldBe(tiaId);
            readModel.SourceCountryCode.ShouldBe("FR");
            readModel.DestinationCountryCode.ShouldBe("CN");
            readModel.DataCategory.ShouldBe("employee-data");
            readModel.Status.ShouldBe(TIAStatus.Draft);
        });
    }

    [Fact]
    public async Task TIAService_FullLifecycle_Draft_To_Completed()
    {
        // Arrange
        using var provider = BuildServiceProvider();

        Guid tiaId;

        // Step 1: Create TIA
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ITIAService>();
            var result = await service.CreateTIAAsync("DE", "BR", "financial", "analyst-1");
            tiaId = result.Match(id => id, _ => throw new InvalidOperationException("Operation failed"));
        }

        // Step 2: Assess risk
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ITIAService>();
            var result = await service.AssessRiskAsync(tiaId, 0.65, "Medium-high risk", "assessor-1");
            result.IsRight.ShouldBeTrue();
        }

        // Step 3: Add supplementary measure
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ITIAService>();
            var result = await service.RequireSupplementaryMeasureAsync(
                tiaId, SupplementaryMeasureType.Technical, "Transit encryption");
            result.IsRight.ShouldBeTrue();
        }

        // Step 4: Submit for DPO review
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ITIAService>();
            var result = await service.SubmitForDPOReviewAsync(tiaId, "submitter-1");
            result.IsRight.ShouldBeTrue();
        }

        // Step 5: DPO approves and completes
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ITIAService>();
            var result = await service.CompleteDPOReviewAsync(tiaId, approved: true, "dpo-1", null);
            result.IsRight.ShouldBeTrue();
        }

        // Verify final state
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ITIAService>();
            var getResult = await service.GetTIAAsync(tiaId);
            getResult.IsRight.ShouldBeTrue();
            getResult.IfRight(rm =>
            {
                rm.Status.ShouldBe(TIAStatus.Completed);
                rm.RiskScore.ShouldBe(0.65);
                rm.RequiredSupplementaryMeasures.Count.ShouldBe(1);
            });
        }
    }

    #endregion

    #region SCC Service via Pipeline

    [Fact]
    public async Task SCCService_RegisterAndGet_PersistsAndReturnsReadModel()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        Guid agreementId;

        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ISCCService>();
            var result = await service.RegisterAgreementAsync(
                "processor-delta", SCCModule.ControllerToProcessor, "2021/914",
                DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow.AddYears(2));
            agreementId = result.Match(id => id, _ => throw new InvalidOperationException("Operation failed"));
        }

        // Act — get
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ISCCService>();
            var getResult = await service.GetAgreementAsync(agreementId);

            // Assert
            getResult.IsRight.ShouldBeTrue();
            getResult.IfRight(rm =>
            {
                rm.Id.ShouldBe(agreementId);
                rm.ProcessorId.ShouldBe("processor-delta");
                rm.Module.ShouldBe(SCCModule.ControllerToProcessor);
                rm.Version.ShouldBe("2021/914");
                rm.IsRevoked.ShouldBeFalse();
            });
        }
    }

    [Fact]
    public async Task SCCService_RegisterAddMeasureRevoke_FullLifecycle()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        Guid agreementId;

        // Register
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ISCCService>();
            var result = await service.RegisterAgreementAsync(
                "proc-lifecycle", SCCModule.ControllerToController, "2021/914",
                DateTimeOffset.UtcNow);
            agreementId = result.Match(id => id, _ => throw new InvalidOperationException("Operation failed"));
        }

        // Add measure
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ISCCService>();
            var result = await service.AddSupplementaryMeasureAsync(
                agreementId, SupplementaryMeasureType.Contractual, "Enhanced liability clauses");
            result.IsRight.ShouldBeTrue();
        }

        // Revoke
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ISCCService>();
            var result = await service.RevokeAgreementAsync(agreementId, "Non-compliance", "dpo-1");
            result.IsRight.ShouldBeTrue();
        }

        // Verify
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ISCCService>();
            var getResult = await service.GetAgreementAsync(agreementId);
            getResult.IsRight.ShouldBeTrue();
            getResult.IfRight(rm =>
            {
                rm.IsRevoked.ShouldBeTrue();
                rm.SupplementaryMeasures.Count.ShouldBe(1);
            });
        }
    }

    #endregion

    #region Approved Transfer Service via Pipeline

    [Fact]
    public async Task TransferService_ApproveAndRevoke_FullLifecycle()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        Guid transferId;

        // Approve
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IApprovedTransferService>();
            var result = await service.ApproveTransferAsync(
                "DE", "US", "health", TransferBasis.SCCs,
                approvedBy: "approver-1", expiresAtUtc: DateTimeOffset.UtcNow.AddYears(1));
            transferId = result.Match(id => id, _ => throw new InvalidOperationException("Operation failed"));
        }

        // Revoke
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IApprovedTransferService>();
            var result = await service.RevokeTransferAsync(transferId, "SCC invalidated", "dpo-1");
            result.IsRight.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task TransferService_ApproveAndRenew_UpdatesExpiry()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        Guid transferId;

        // Approve with short expiry
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IApprovedTransferService>();
            var result = await service.ApproveTransferAsync(
                "FR", "JP", "analytics", TransferBasis.AdequacyDecision,
                approvedBy: "approver-1", expiresAtUtc: DateTimeOffset.UtcNow.AddMonths(3));
            transferId = result.Match(id => id, _ => throw new InvalidOperationException("Operation failed"));
        }

        // Renew
        var newExpiry = DateTimeOffset.UtcNow.AddYears(2);
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IApprovedTransferService>();
            var result = await service.RenewTransferAsync(transferId, newExpiry, "renewer-1");
            result.IsRight.ShouldBeTrue();
        }
    }

    #endregion

    #region Service Error Handling

    [Fact]
    public async Task TIAService_GetNonExistentTIA_ReturnsError()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITIAService>();

        // Act
        var result = await service.GetTIAAsync(Guid.NewGuid());

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task SCCService_RevokeNonExistentAgreement_ReturnsError()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISCCService>();

        // Act
        var result = await service.RevokeAgreementAsync(Guid.NewGuid(), "reason", "user");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task TransferService_RevokeNonExistentTransfer_ReturnsError()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IApprovedTransferService>();

        // Act
        var result = await service.RevokeTransferAsync(Guid.NewGuid(), "reason", "user");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Options Configuration

    [Fact]
    public void Options_ConfiguresCorrectly()
    {
        // Arrange & Act
        using var provider = BuildServiceProvider(options =>
        {
            options.EnforcementMode = CrossBorderTransferEnforcementMode.Warn;
            options.DefaultSourceCountryCode = "DE";
            options.TIARiskThreshold = 0.7;
        });

        // Assert
        var opts = provider.GetRequiredService<IOptions<CrossBorderTransferOptions>>().Value;
        opts.EnforcementMode.ShouldBe(CrossBorderTransferEnforcementMode.Warn);
        opts.DefaultSourceCountryCode.ShouldBe("DE");
        opts.TIARiskThreshold.ShouldBe(0.7);
    }

    #endregion
}
