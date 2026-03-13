#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Health;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.Scheduling;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Encina.IntegrationTests.Compliance.ProcessorAgreements;

/// <summary>
/// Integration tests for the Processor Agreements pipeline verifying DI registration,
/// options configuration, store roundtrips, pipeline enforcement behavior,
/// expiration monitoring, health check, and concurrent access patterns using in-memory stores.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ProcessorAgreementPipelineIntegrationTests
{
    #region DI Registration

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersIProcessorRegistry()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements();
        var provider = services.BuildServiceProvider();

        var registry = provider.GetService<IProcessorRegistry>();
        registry.Should().NotBeNull();
        registry.Should().BeOfType<InMemoryProcessorRegistry>();
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersIDPAStore()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements();
        var provider = services.BuildServiceProvider();

        var store = provider.GetService<IDPAStore>();
        store.Should().NotBeNull();
        store.Should().BeOfType<InMemoryDPAStore>();
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersIProcessorAuditStore()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements();
        var provider = services.BuildServiceProvider();

        var auditStore = provider.GetService<IProcessorAuditStore>();
        auditStore.Should().NotBeNull();
        auditStore.Should().BeOfType<InMemoryProcessorAuditStore>();
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersIDPAValidator()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements();
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetService<IDPAValidator>();
        validator.Should().NotBeNull();
        validator.Should().BeOfType<DefaultDPAValidator>();
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersPipelineBehavior()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements();

        services.Should().Contain(
            d => d.ServiceType == typeof(IPipelineBehavior<,>)
              && d.ImplementationType == typeof(ProcessorValidationPipelineBehavior<,>));
    }

    [Fact]
    public void AddEncinaProcessorAgreements_StoresAreSingletons()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements();
        var provider = services.BuildServiceProvider();

        var registry1 = provider.GetService<IProcessorRegistry>();
        var registry2 = provider.GetService<IProcessorRegistry>();
        registry1.Should().BeSameAs(registry2);

        var store1 = provider.GetService<IDPAStore>();
        var store2 = provider.GetService<IDPAStore>();
        store1.Should().BeSameAs(store2);

        var audit1 = provider.GetService<IProcessorAuditStore>();
        var audit2 = provider.GetService<IProcessorAuditStore>();
        audit1.Should().BeSameAs(audit2);
    }

    [Fact]
    public void AddEncinaProcessorAgreements_ValidatorIsScoped_DifferentPerScope()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements();
        var provider = services.BuildServiceProvider();

        IDPAValidator? validator1, validator2;

        using (var scope1 = provider.CreateScope())
        {
            validator1 = scope1.ServiceProvider.GetService<IDPAValidator>();
        }

        using (var scope2 = provider.CreateScope())
        {
            validator2 = scope2.ServiceProvider.GetService<IDPAValidator>();
        }

        validator1.Should().NotBeSameAs(validator2);
    }

    [Fact]
    public void AddEncinaProcessorAgreements_RegistersExpirationHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements();

        services.Should().Contain(
            d => d.ServiceType == typeof(ICommandHandler<CheckDPAExpirationCommand, Unit>)
              && d.ImplementationType == typeof(CheckDPAExpirationHandler));
    }

    #endregion

    #region Options Configuration

    [Fact]
    public void AddEncinaProcessorAgreements_DefaultOptions_HaveCorrectValues()
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
    public void AddEncinaProcessorAgreements_CustomOptions_AreApplied()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements(options =>
        {
            options.EnforcementMode = ProcessorAgreementEnforcementMode.Block;
            options.MaxSubProcessorDepth = 5;
            options.EnableExpirationMonitoring = true;
            options.ExpirationCheckInterval = TimeSpan.FromMinutes(30);
            options.ExpirationWarningDays = 60;
            options.TrackAuditTrail = false;
        });
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<ProcessorAgreementOptions>>().Value;

        options.EnforcementMode.Should().Be(ProcessorAgreementEnforcementMode.Block);
        options.BlockWithoutValidDPA.Should().BeTrue();
        options.MaxSubProcessorDepth.Should().Be(5);
        options.EnableExpirationMonitoring.Should().BeTrue();
        options.ExpirationCheckInterval.Should().Be(TimeSpan.FromMinutes(30));
        options.ExpirationWarningDays.Should().Be(60);
        options.TrackAuditTrail.Should().BeFalse();
    }

    [Fact]
    public void AddEncinaProcessorAgreements_BlockWithoutValidDPA_SetsEnforcementModeToBlock()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements(options =>
        {
            options.BlockWithoutValidDPA = true;
        });
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<ProcessorAgreementOptions>>().Value;

        options.EnforcementMode.Should().Be(ProcessorAgreementEnforcementMode.Block);
    }

    #endregion

    #region Processor Registry Lifecycle

    [Fact]
    public async Task ProcessorRegistry_RegisterAndRetrieve_Roundtrip()
    {
        var provider = BuildProvider();
        var registry = provider.GetRequiredService<IProcessorRegistry>();

        var processor = CreateProcessor("test-registry-roundtrip", "Roundtrip Processor");
        var registerResult = await registry.RegisterProcessorAsync(processor);
        registerResult.IsRight.Should().BeTrue("registration should succeed");

        var getResult = await registry.GetProcessorAsync("test-registry-roundtrip");
        getResult.IsRight.Should().BeTrue("get should succeed");

        var option = getResult.Match(Right: o => o, Left: _ => Option<Processor>.None);
        option.IsSome.Should().BeTrue("processor should be found");
        option.IfSome(p =>
        {
            p.Name.Should().Be("Roundtrip Processor");
            p.Country.Should().Be("EU");
        });
    }

    [Fact]
    public async Task ProcessorRegistry_GetAllProcessors_ReturnsAllRegistered()
    {
        var provider = BuildProvider();
        var registry = provider.GetRequiredService<IProcessorRegistry>();

        await registry.RegisterProcessorAsync(CreateProcessor("proc-all-a", "Processor A"));
        await registry.RegisterProcessorAsync(CreateProcessor("proc-all-b", "Processor B"));
        await registry.RegisterProcessorAsync(CreateProcessor("proc-all-c", "Processor C"));

        var result = await registry.GetAllProcessorsAsync();
        result.IsRight.Should().BeTrue();

        var all = result.Match(Right: list => list, Left: _ => []);
        all.Count.Should().Be(3);
    }

    [Fact]
    public async Task ProcessorRegistry_UpdateProcessor_ReflectsChanges()
    {
        var provider = BuildProvider();
        var registry = provider.GetRequiredService<IProcessorRegistry>();

        var processor = CreateProcessor("proc-update", "Original Name");
        await registry.RegisterProcessorAsync(processor);

        var updated = processor with { Name = "Updated Name", LastUpdatedAtUtc = DateTimeOffset.UtcNow };
        var updateResult = await registry.UpdateProcessorAsync(updated);
        updateResult.IsRight.Should().BeTrue();

        var getResult = await registry.GetProcessorAsync("proc-update");
        var option = getResult.Match(Right: o => o, Left: _ => Option<Processor>.None);
        option.IfSome(p => p.Name.Should().Be("Updated Name"));
    }

    [Fact]
    public async Task ProcessorRegistry_RemoveProcessor_MakesItUnfindable()
    {
        var provider = BuildProvider();
        var registry = provider.GetRequiredService<IProcessorRegistry>();

        await registry.RegisterProcessorAsync(CreateProcessor("proc-remove", "To Remove"));

        var removeResult = await registry.RemoveProcessorAsync("proc-remove");
        removeResult.IsRight.Should().BeTrue();

        var getResult = await registry.GetProcessorAsync("proc-remove");
        var option = getResult.Match(Right: o => o, Left: _ => Option<Processor>.None);
        option.IsNone.Should().BeTrue("removed processor should not be found");
    }

    [Fact]
    public async Task ProcessorRegistry_SubProcessorHierarchy_GetSubProcessorsReturnsDirectChildren()
    {
        var provider = BuildProvider();
        var registry = provider.GetRequiredService<IProcessorRegistry>();
        var now = DateTimeOffset.UtcNow;

        var parent = CreateProcessor("parent-proc", "Parent");
        await registry.RegisterProcessorAsync(parent);

        var child1 = new Processor
        {
            Id = "child-proc-1",
            Name = "Child 1",
            Country = "US",
            ParentProcessorId = "parent-proc",
            Depth = 1,
            SubProcessorAuthorizationType = SubProcessorAuthorizationType.General,
            CreatedAtUtc = now,
            LastUpdatedAtUtc = now
        };
        var child2 = new Processor
        {
            Id = "child-proc-2",
            Name = "Child 2",
            Country = "UK",
            ParentProcessorId = "parent-proc",
            Depth = 1,
            SubProcessorAuthorizationType = SubProcessorAuthorizationType.Specific,
            CreatedAtUtc = now,
            LastUpdatedAtUtc = now
        };

        await registry.RegisterProcessorAsync(child1);
        await registry.RegisterProcessorAsync(child2);

        var subResult = await registry.GetSubProcessorsAsync("parent-proc");
        subResult.IsRight.Should().BeTrue();

        var subs = subResult.Match(Right: list => list, Left: _ => []);
        subs.Count.Should().Be(2);
        subs.Should().Contain(p => p.Id == "child-proc-1");
        subs.Should().Contain(p => p.Id == "child-proc-2");
    }

    [Fact]
    public async Task ProcessorRegistry_GetFullSubProcessorChain_ReturnsEntireHierarchy()
    {
        var provider = BuildProvider();
        var registry = provider.GetRequiredService<IProcessorRegistry>();
        var now = DateTimeOffset.UtcNow;

        await registry.RegisterProcessorAsync(CreateProcessor("chain-root", "Root"));

        await registry.RegisterProcessorAsync(new Processor
        {
            Id = "chain-level1",
            Name = "Level 1",
            Country = "US",
            ParentProcessorId = "chain-root",
            Depth = 1,
            SubProcessorAuthorizationType = SubProcessorAuthorizationType.General,
            CreatedAtUtc = now,
            LastUpdatedAtUtc = now
        });

        await registry.RegisterProcessorAsync(new Processor
        {
            Id = "chain-level2",
            Name = "Level 2",
            Country = "JP",
            ParentProcessorId = "chain-level1",
            Depth = 2,
            SubProcessorAuthorizationType = SubProcessorAuthorizationType.Specific,
            CreatedAtUtc = now,
            LastUpdatedAtUtc = now
        });

        var chainResult = await registry.GetFullSubProcessorChainAsync("chain-root");
        chainResult.IsRight.Should().BeTrue();

        var chain = chainResult.Match(Right: list => list, Left: _ => []);
        chain.Count.Should().Be(2);
        chain.Should().Contain(p => p.Id == "chain-level1");
        chain.Should().Contain(p => p.Id == "chain-level2");
    }

    #endregion

    #region DPA Store Lifecycle

    [Fact]
    public async Task DPAStore_AddAndRetrieveById_Roundtrip()
    {
        var provider = BuildProvider();
        var registry = provider.GetRequiredService<IProcessorRegistry>();
        var store = provider.GetRequiredService<IDPAStore>();

        await registry.RegisterProcessorAsync(CreateProcessor("dpa-roundtrip-proc", "DPA Roundtrip Processor"));

        var dpa = CreateDPA("dpa-roundtrip-1", "dpa-roundtrip-proc");
        var addResult = await store.AddAsync(dpa);
        addResult.IsRight.Should().BeTrue("add should succeed");

        var getResult = await store.GetByIdAsync("dpa-roundtrip-1");
        getResult.IsRight.Should().BeTrue();

        var option = getResult.Match(Right: o => o, Left: _ => Option<DataProcessingAgreement>.None);
        option.IsSome.Should().BeTrue("DPA should be found");
        option.IfSome(a =>
        {
            a.ProcessorId.Should().Be("dpa-roundtrip-proc");
            a.Status.Should().Be(DPAStatus.Active);
        });
    }

    [Fact]
    public async Task DPAStore_GetByProcessorId_ReturnsAllDPAsForProcessor()
    {
        var provider = BuildProvider();
        var registry = provider.GetRequiredService<IProcessorRegistry>();
        var store = provider.GetRequiredService<IDPAStore>();

        await registry.RegisterProcessorAsync(CreateProcessor("dpa-history-proc", "History Processor"));

        var activeDpa = CreateDPA("dpa-hist-active", "dpa-history-proc", DPAStatus.Active);
        var expiredDpa = CreateDPA("dpa-hist-expired", "dpa-history-proc", DPAStatus.Expired);

        await store.AddAsync(activeDpa);
        await store.AddAsync(expiredDpa);

        var result = await store.GetByProcessorIdAsync("dpa-history-proc");
        result.IsRight.Should().BeTrue();

        var all = result.Match(Right: list => list, Left: _ => []);
        all.Count.Should().Be(2);
    }

    [Fact]
    public async Task DPAStore_GetActiveByProcessorId_ReturnsOnlyActiveDPA()
    {
        var provider = BuildProvider();
        var registry = provider.GetRequiredService<IProcessorRegistry>();
        var store = provider.GetRequiredService<IDPAStore>();

        await registry.RegisterProcessorAsync(CreateProcessor("dpa-active-proc", "Active DPA Processor"));

        var activeDpa = CreateDPA("dpa-act-1", "dpa-active-proc", DPAStatus.Active);
        var terminatedDpa = CreateDPA("dpa-act-2", "dpa-active-proc", DPAStatus.Terminated);

        await store.AddAsync(activeDpa);
        await store.AddAsync(terminatedDpa);

        var result = await store.GetActiveByProcessorIdAsync("dpa-active-proc");
        result.IsRight.Should().BeTrue();

        var option = result.Match(Right: o => o, Left: _ => Option<DataProcessingAgreement>.None);
        option.IsSome.Should().BeTrue("should find active DPA");
        option.IfSome(a => a.Id.Should().Be("dpa-act-1"));
    }

    [Fact]
    public async Task DPAStore_GetByStatus_ReturnsMatchingOnly()
    {
        var provider = BuildProvider();
        var store = provider.GetRequiredService<IDPAStore>();

        await store.AddAsync(CreateDPA("status-active", "proc-x", DPAStatus.Active));
        await store.AddAsync(CreateDPA("status-expired", "proc-y", DPAStatus.Expired));
        await store.AddAsync(CreateDPA("status-terminated", "proc-z", DPAStatus.Terminated));

        var result = await store.GetByStatusAsync(DPAStatus.Active);
        result.IsRight.Should().BeTrue();

        var active = result.Match(Right: list => list, Left: _ => []);
        active.Count.Should().Be(1);
        active[0].Id.Should().Be("status-active");
    }

    [Fact]
    public async Task DPAStore_GetExpiring_ReturnsAgreementsExpiringBeforeThreshold()
    {
        var provider = BuildProvider();
        var store = provider.GetRequiredService<IDPAStore>();
        var now = DateTimeOffset.UtcNow;

        // Expiring in 15 days (within 30-day threshold)
        await store.AddAsync(CreateDPA("exp-soon", "proc-s", DPAStatus.Active, now.AddDays(15)));
        // Expiring in 60 days (outside 30-day threshold)
        await store.AddAsync(CreateDPA("exp-later", "proc-l", DPAStatus.Active, now.AddDays(60)));
        // Already expired
        await store.AddAsync(CreateDPA("exp-past", "proc-p", DPAStatus.Active, now.AddDays(-5)));

        var threshold = now.AddDays(30);
        var result = await store.GetExpiringAsync(threshold);
        result.IsRight.Should().BeTrue();

        var expiring = result.Match(Right: list => list, Left: _ => []);
        expiring.Count.Should().Be(2, "should include both the soon-expiring and already-expired active DPAs");
        expiring.Should().Contain(a => a.Id == "exp-soon");
        expiring.Should().Contain(a => a.Id == "exp-past");
    }

    [Fact]
    public async Task DPAStore_Update_ModifiesExistingAgreement()
    {
        var provider = BuildProvider();
        var store = provider.GetRequiredService<IDPAStore>();

        var dpa = CreateDPA("dpa-upd-1", "proc-upd");
        await store.AddAsync(dpa);

        var updated = dpa with
        {
            Status = DPAStatus.Terminated,
            LastUpdatedAtUtc = DateTimeOffset.UtcNow
        };
        var updateResult = await store.UpdateAsync(updated);
        updateResult.IsRight.Should().BeTrue();

        var getResult = await store.GetByIdAsync("dpa-upd-1");
        var option = getResult.Match(Right: o => o, Left: _ => Option<DataProcessingAgreement>.None);
        option.IfSome(a => a.Status.Should().Be(DPAStatus.Terminated));
    }

    #endregion

    #region Audit Store

    [Fact]
    public async Task AuditStore_RecordAndRetrieve_Roundtrip()
    {
        var provider = BuildProvider();
        var auditStore = provider.GetRequiredService<IProcessorAuditStore>();

        var entry = new ProcessorAgreementAuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            ProcessorId = "audit-proc",
            Action = "Registered",
            Detail = "Processor registered during integration test.",
            PerformedByUserId = "integration-test",
            OccurredAtUtc = DateTimeOffset.UtcNow
        };

        var recordResult = await auditStore.RecordAsync(entry);
        recordResult.IsRight.Should().BeTrue("audit record should succeed");

        var trailResult = await auditStore.GetAuditTrailAsync("audit-proc");
        trailResult.IsRight.Should().BeTrue();

        var trail = trailResult.Match(
            Right: entries => entries,
            Left: _ => (IReadOnlyList<ProcessorAgreementAuditEntry>)[]);
        trail.Count.Should().BeGreaterThanOrEqualTo(1);
        trail.Should().Contain(e => e.Action == "Registered");
    }

    [Fact]
    public async Task AuditStore_MultipleEntries_ReturnedInOrder()
    {
        var provider = BuildProvider();
        var auditStore = provider.GetRequiredService<IProcessorAuditStore>();
        var processorId = $"audit-order-{Guid.NewGuid():N}";

        var entry1 = new ProcessorAgreementAuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            ProcessorId = processorId,
            Action = "Registered",
            OccurredAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10)
        };
        var entry2 = new ProcessorAgreementAuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            ProcessorId = processorId,
            DPAId = "dpa-1",
            Action = "DPASigned",
            OccurredAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        var entry3 = new ProcessorAgreementAuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            ProcessorId = processorId,
            Action = "SubProcessorAdded",
            OccurredAtUtc = DateTimeOffset.UtcNow
        };

        await auditStore.RecordAsync(entry1);
        await auditStore.RecordAsync(entry2);
        await auditStore.RecordAsync(entry3);

        var trailResult = await auditStore.GetAuditTrailAsync(processorId);
        var trail = trailResult.Match(
            Right: entries => entries,
            Left: _ => (IReadOnlyList<ProcessorAgreementAuditEntry>)[]);

        trail.Count.Should().Be(3);
        trail[0].Action.Should().Be("Registered");
        trail[1].Action.Should().Be("DPASigned");
        trail[2].Action.Should().Be("SubProcessorAdded");
    }

    #endregion

    #region DPA Validator

    [Fact]
    public async Task Validator_HasValidDPA_ReturnsTrueForProcessorWithActiveDPA()
    {
        var provider = BuildProvider();
        var registry = provider.GetRequiredService<IProcessorRegistry>();
        var store = provider.GetRequiredService<IDPAStore>();

        await registry.RegisterProcessorAsync(CreateProcessor("valid-proc", "Valid Processor"));
        await store.AddAsync(CreateFullyCompliantDPA("valid-dpa", "valid-proc"));

        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<IDPAValidator>();

        var result = await validator.HasValidDPAAsync("valid-proc");
        result.IsRight.Should().BeTrue();

        var isValid = result.Match(Right: v => v, Left: _ => false);
        isValid.Should().BeTrue("processor with active, fully compliant DPA should be valid");
    }

    [Fact]
    public async Task Validator_HasValidDPA_ReturnsFalseForProcessorWithoutDPA()
    {
        var provider = BuildProvider();
        var registry = provider.GetRequiredService<IProcessorRegistry>();

        await registry.RegisterProcessorAsync(CreateProcessor("no-dpa-proc", "No DPA Processor"));

        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<IDPAValidator>();

        var result = await validator.HasValidDPAAsync("no-dpa-proc");
        result.IsRight.Should().BeTrue();

        var isValid = result.Match(Right: v => v, Left: _ => true);
        isValid.Should().BeFalse("processor without any DPA should not be valid");
    }

    [Fact]
    public async Task Validator_ValidateAsync_ReturnsDetailedResultWithMissingTerms()
    {
        var provider = BuildProvider();
        var registry = provider.GetRequiredService<IProcessorRegistry>();
        var store = provider.GetRequiredService<IDPAStore>();

        await registry.RegisterProcessorAsync(CreateProcessor("incomplete-proc", "Incomplete Processor"));

        // DPA with some missing mandatory terms
        var incompleteDpa = new DataProcessingAgreement
        {
            Id = "incomplete-dpa",
            ProcessorId = "incomplete-proc",
            Status = DPAStatus.Active,
            SignedAtUtc = DateTimeOffset.UtcNow.AddMonths(-1),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddYears(1),
            MandatoryTerms = new DPAMandatoryTerms
            {
                ProcessOnDocumentedInstructions = true,
                ConfidentialityObligations = true,
                SecurityMeasures = true,
                SubProcessorRequirements = false,  // Missing
                DataSubjectRightsAssistance = true,
                ComplianceAssistance = false,       // Missing
                DataDeletionOrReturn = true,
                AuditRights = true
            },
            HasSCCs = false,
            ProcessingPurposes = ["Data analysis"],
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMonths(-1),
            LastUpdatedAtUtc = DateTimeOffset.UtcNow.AddMonths(-1)
        };
        await store.AddAsync(incompleteDpa);

        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<IDPAValidator>();

        var result = await validator.ValidateAsync("incomplete-proc");
        result.IsRight.Should().BeTrue();

        var validation = result.Match(Right: v => v, Left: _ => null!);
        validation.Should().NotBeNull();
        validation.IsValid.Should().BeFalse("DPA with missing terms should not be valid");
        validation.MissingTerms.Should().Contain("SubProcessorRequirements");
        validation.MissingTerms.Should().Contain("ComplianceAssistance");
    }

    [Fact]
    public async Task Validator_ValidateAllAsync_ReturnsOneResultPerProcessor()
    {
        var provider = BuildProvider();
        var registry = provider.GetRequiredService<IProcessorRegistry>();
        var store = provider.GetRequiredService<IDPAStore>();

        await registry.RegisterProcessorAsync(CreateProcessor("all-val-1", "Processor 1"));
        await registry.RegisterProcessorAsync(CreateProcessor("all-val-2", "Processor 2"));
        await store.AddAsync(CreateFullyCompliantDPA("all-val-dpa-1", "all-val-1"));
        // Processor 2 has no DPA

        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<IDPAValidator>();

        var result = await validator.ValidateAllAsync();
        result.IsRight.Should().BeTrue();

        var results = result.Match(Right: list => list, Left: _ => []);
        results.Count.Should().Be(2);
        results.Should().Contain(r => r.ProcessorId == "all-val-1" && r.IsValid);
        results.Should().Contain(r => r.ProcessorId == "all-val-2" && !r.IsValid);
    }

    #endregion

    #region Full Lifecycle: Register -> DPA -> Validate -> Audit

    [Fact]
    public async Task FullLifecycle_RegisterProcessor_SignDPA_Validate_Audit()
    {
        var provider = BuildProvider();
        var registry = provider.GetRequiredService<IProcessorRegistry>();
        var store = provider.GetRequiredService<IDPAStore>();
        var auditStore = provider.GetRequiredService<IProcessorAuditStore>();

        // Step 1: Register processor
        var processor = CreateProcessor("lifecycle-proc", "Lifecycle Processor");
        var registerResult = await registry.RegisterProcessorAsync(processor);
        registerResult.IsRight.Should().BeTrue();

        // Step 2: Save DPA with full compliance
        var dpa = CreateFullyCompliantDPA("lifecycle-dpa", "lifecycle-proc");
        var addResult = await store.AddAsync(dpa);
        addResult.IsRight.Should().BeTrue();

        // Step 3: Validate processor has valid DPA
        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<IDPAValidator>();

        var hasValid = await validator.HasValidDPAAsync("lifecycle-proc");
        var isValid = hasValid.Match(Right: v => v, Left: _ => false);
        isValid.Should().BeTrue("processor with fully compliant active DPA should be valid");

        var detailedValidation = await validator.ValidateAsync("lifecycle-proc");
        var validation = detailedValidation.Match(Right: v => v, Left: _ => null!);
        validation.IsValid.Should().BeTrue();
        validation.MissingTerms.Should().BeEmpty();
        validation.DPAId.Should().Be("lifecycle-dpa");

        // Step 4: Record audit entry
        var auditEntry = new ProcessorAgreementAuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            ProcessorId = "lifecycle-proc",
            DPAId = "lifecycle-dpa",
            Action = "DPASigned",
            Detail = "DPA signed during integration test lifecycle.",
            PerformedByUserId = "integration-test",
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
        var auditResult = await auditStore.RecordAsync(auditEntry);
        auditResult.IsRight.Should().BeTrue();

        // Step 5: Verify audit trail
        var trailResult = await auditStore.GetAuditTrailAsync("lifecycle-proc");
        var trail = trailResult.Match(
            Right: entries => entries,
            Left: _ => (IReadOnlyList<ProcessorAgreementAuditEntry>)[]);
        trail.Count.Should().BeGreaterThanOrEqualTo(1);
        trail.Should().Contain(e => e.Action == "DPASigned");
    }

    #endregion

    #region Expiration Handler

    [Fact]
    public async Task ExpirationHandler_DetectsExpiredDPAs_TransitionsStatusAndPublishesNotification()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements(options =>
        {
            options.EnableExpirationMonitoring = true;
            options.ExpirationWarningDays = 30;
            options.TrackAuditTrail = true;
        });

        // Register FakeEncina for notification publishing
        var fakeEncina = new FakeEncina();
        services.AddSingleton<IEncina>(fakeEncina);

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IProcessorRegistry>();
        var store = provider.GetRequiredService<IDPAStore>();
        var now = DateTimeOffset.UtcNow;

        // Register processor and expired DPA
        await registry.RegisterProcessorAsync(CreateProcessor("exp-handler-proc", "Expiration Handler Processor"));

        var expiredDpa = new DataProcessingAgreement
        {
            Id = "exp-handler-dpa",
            ProcessorId = "exp-handler-proc",
            Status = DPAStatus.Active,
            SignedAtUtc = now.AddYears(-2),
            ExpiresAtUtc = now.AddDays(-5),  // Already expired
            MandatoryTerms = CreateFullCompliantTerms(),
            HasSCCs = true,
            ProcessingPurposes = ["Payment processing"],
            CreatedAtUtc = now.AddYears(-2),
            LastUpdatedAtUtc = now.AddYears(-2)
        };
        await store.AddAsync(expiredDpa);

        // Run the expiration handler
        var handler = provider.GetRequiredService<ICommandHandler<CheckDPAExpirationCommand, Unit>>();
        var handleResult = await handler.Handle(new CheckDPAExpirationCommand(), CancellationToken.None);
        handleResult.IsRight.Should().BeTrue("expiration handler should succeed");

        // Verify DPA status was transitioned to Expired
        var updatedDpa = await store.GetByIdAsync("exp-handler-dpa");
        var dpaOption = updatedDpa.Match(Right: o => o, Left: _ => Option<DataProcessingAgreement>.None);
        dpaOption.IfSome(a => a.Status.Should().Be(DPAStatus.Expired));

        // Verify audit trail has expiration entry
        var auditStore = provider.GetRequiredService<IProcessorAuditStore>();
        var trail = await auditStore.GetAuditTrailAsync("exp-handler-proc");
        var entries = trail.Match(
            Right: list => list,
            Left: _ => (IReadOnlyList<ProcessorAgreementAuditEntry>)[]);
        entries.Should().Contain(e => e.Action == "DPAExpired");
    }

    [Fact]
    public async Task ExpirationHandler_DetectsExpiringDPAs_PublishesWarningNotification()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements(options =>
        {
            options.EnableExpirationMonitoring = true;
            options.ExpirationWarningDays = 30;
            options.TrackAuditTrail = true;
        });

        var fakeEncina = new FakeEncina();
        services.AddSingleton<IEncina>(fakeEncina);

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IProcessorRegistry>();
        var store = provider.GetRequiredService<IDPAStore>();
        var now = DateTimeOffset.UtcNow;

        await registry.RegisterProcessorAsync(CreateProcessor("exp-warn-proc", "Expiring Soon Processor"));

        var expiringDpa = new DataProcessingAgreement
        {
            Id = "exp-warn-dpa",
            ProcessorId = "exp-warn-proc",
            Status = DPAStatus.Active,
            SignedAtUtc = now.AddYears(-1),
            ExpiresAtUtc = now.AddDays(15),  // Expiring in 15 days (within 30-day warning)
            MandatoryTerms = CreateFullCompliantTerms(),
            HasSCCs = true,
            ProcessingPurposes = ["Analytics"],
            CreatedAtUtc = now.AddYears(-1),
            LastUpdatedAtUtc = now.AddYears(-1)
        };
        await store.AddAsync(expiringDpa);

        var handler = provider.GetRequiredService<ICommandHandler<CheckDPAExpirationCommand, Unit>>();
        var handleResult = await handler.Handle(new CheckDPAExpirationCommand(), CancellationToken.None);
        handleResult.IsRight.Should().BeTrue();

        // DPA should still be Active (not yet expired)
        var dpaResult = await store.GetByIdAsync("exp-warn-dpa");
        var dpaOption = dpaResult.Match(Right: o => o, Left: _ => Option<DataProcessingAgreement>.None);
        dpaOption.IfSome(a => a.Status.Should().Be(DPAStatus.Active));

        // Verify audit trail has expiring warning entry
        var auditStore = provider.GetRequiredService<IProcessorAuditStore>();
        var trail = await auditStore.GetAuditTrailAsync("exp-warn-proc");
        var entries = trail.Match(
            Right: list => list,
            Left: _ => (IReadOnlyList<ProcessorAgreementAuditEntry>)[]);
        entries.Should().Contain(e => e.Action == "DPAExpiring");
    }

    #endregion

    #region Health Check

    [Fact]
    public void AddEncinaProcessorAgreements_WithHealthCheck_RegistersHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements(options =>
        {
            options.AddHealthCheck = true;
        });
        var provider = services.BuildServiceProvider();

        var healthCheckService = provider.GetService<HealthCheckService>();
        healthCheckService.Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaProcessorAgreements_WithoutHealthCheck_DoesNotRegisterHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements(options =>
        {
            options.AddHealthCheck = false;
        });

        services.Any(d => d.ServiceType == typeof(HealthCheckService))
            .Should().BeFalse();
    }

    #endregion

    #region Custom Store Override

    [Fact]
    public void AddEncinaProcessorAgreements_CustomStoreRegisteredBefore_TryAddDoesNotOverride()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Register custom store BEFORE AddEncinaProcessorAgreements
        services.AddSingleton<IDPAStore, InMemoryDPAStore>();
        services.AddEncinaProcessorAgreements();

        var provider = services.BuildServiceProvider();
        var store = provider.GetService<IDPAStore>();

        // Should still be the first-registered InMemoryDPAStore (TryAdd does not override)
        store.Should().NotBeNull();
        store.Should().BeOfType<InMemoryDPAStore>();
    }

    #endregion

    #region Concurrent Access

    [Fact]
    public async Task InMemoryStores_ConcurrentAccess_ThreadSafe()
    {
        var provider = BuildProvider();
        var registry = provider.GetRequiredService<IProcessorRegistry>();
        var auditStore = provider.GetRequiredService<IProcessorAuditStore>();

        const int concurrentOps = 50;

        // Concurrent processor registrations
        var registerTasks = Enumerable.Range(0, concurrentOps)
            .Select(i => registry.RegisterProcessorAsync(
                CreateProcessor($"conc-proc-{i}", $"Concurrent Processor {i}")).AsTask());

        await Task.WhenAll(registerTasks);

        var allResult = await registry.GetAllProcessorsAsync();
        var all = allResult.Match(Right: list => list, Left: _ => []);
        all.Count.Should().Be(concurrentOps);

        // Concurrent audit writes for a single processor
        var auditProcessorId = $"conc-audit-{Guid.NewGuid():N}";
        var auditTasks = Enumerable.Range(0, concurrentOps)
            .Select(i => auditStore.RecordAsync(new ProcessorAgreementAuditEntry
            {
                Id = Guid.NewGuid().ToString(),
                ProcessorId = auditProcessorId,
                Action = $"Action-{i}",
                PerformedByUserId = "concurrent-test",
                OccurredAtUtc = DateTimeOffset.UtcNow
            }).AsTask());

        await Task.WhenAll(auditTasks);

        var trailResult = await auditStore.GetAuditTrailAsync(auditProcessorId);
        var trail = trailResult.Match(
            Right: entries => entries,
            Left: _ => (IReadOnlyList<ProcessorAgreementAuditEntry>)[]);
        trail.Count.Should().Be(concurrentOps);
    }

    #endregion

    #region DataProcessingAgreement.IsActive

    [Fact]
    public void DataProcessingAgreement_IsActive_ReturnsTrueForActiveNotExpired()
    {
        var dpa = CreateDPA("isactive-1", "proc-x", DPAStatus.Active, DateTimeOffset.UtcNow.AddDays(30));
        dpa.IsActive(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void DataProcessingAgreement_IsActive_ReturnsFalseForExpiredDate()
    {
        var dpa = CreateDPA("isactive-2", "proc-x", DPAStatus.Active, DateTimeOffset.UtcNow.AddDays(-5));
        dpa.IsActive(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void DataProcessingAgreement_IsActive_ReturnsFalseForTerminatedStatus()
    {
        var dpa = CreateDPA("isactive-3", "proc-x", DPAStatus.Terminated, DateTimeOffset.UtcNow.AddDays(30));
        dpa.IsActive(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void DataProcessingAgreement_IsActive_ReturnsTrueForNullExpiration()
    {
        var dpa = CreateDPA("isactive-4", "proc-x", DPAStatus.Active, null);
        dpa.IsActive(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    #endregion

    #region DPAMandatoryTerms

    [Fact]
    public void DPAMandatoryTerms_IsFullyCompliant_WhenAllTermsTrue()
    {
        var terms = CreateFullCompliantTerms();
        terms.IsFullyCompliant.Should().BeTrue();
        terms.MissingTerms.Should().BeEmpty();
    }

    [Fact]
    public void DPAMandatoryTerms_MissingTerms_IdentifiesGaps()
    {
        var terms = new DPAMandatoryTerms
        {
            ProcessOnDocumentedInstructions = true,
            ConfidentialityObligations = false,
            SecurityMeasures = true,
            SubProcessorRequirements = true,
            DataSubjectRightsAssistance = true,
            ComplianceAssistance = true,
            DataDeletionOrReturn = false,
            AuditRights = true
        };

        terms.IsFullyCompliant.Should().BeFalse();
        terms.MissingTerms.Should().HaveCount(2);
        terms.MissingTerms.Should().Contain("ConfidentialityObligations");
        terms.MissingTerms.Should().Contain("DataDeletionOrReturn");
    }

    #endregion

    #region Helpers

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaProcessorAgreements();
        return services.BuildServiceProvider();
    }

    private static Processor CreateProcessor(
        string id,
        string name,
        int depth = 0,
        string? parentProcessorId = null) => new()
        {
            Id = id,
            Name = name,
            Country = "EU",
            Depth = depth,
            ParentProcessorId = parentProcessorId,
            SubProcessorAuthorizationType = SubProcessorAuthorizationType.General,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastUpdatedAtUtc = DateTimeOffset.UtcNow
        };

    private static DataProcessingAgreement CreateDPA(
        string id,
        string processorId,
        DPAStatus status = DPAStatus.Active,
        DateTimeOffset? expiresAtUtc = null) => new()
        {
            Id = id,
            ProcessorId = processorId,
            Status = status,
            SignedAtUtc = DateTimeOffset.UtcNow.AddMonths(-6),
            ExpiresAtUtc = expiresAtUtc ?? DateTimeOffset.UtcNow.AddYears(1),
            MandatoryTerms = CreateFullCompliantTerms(),
            HasSCCs = true,
            ProcessingPurposes = ["Data processing"],
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMonths(-6),
            LastUpdatedAtUtc = DateTimeOffset.UtcNow.AddMonths(-6)
        };

    private static DataProcessingAgreement CreateFullyCompliantDPA(
        string id,
        string processorId) => new()
        {
            Id = id,
            ProcessorId = processorId,
            Status = DPAStatus.Active,
            SignedAtUtc = DateTimeOffset.UtcNow.AddMonths(-1),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddYears(1),
            MandatoryTerms = CreateFullCompliantTerms(),
            HasSCCs = true,
            ProcessingPurposes = ["Data processing", "Analytics"],
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMonths(-1),
            LastUpdatedAtUtc = DateTimeOffset.UtcNow.AddMonths(-1)
        };

    private static DPAMandatoryTerms CreateFullCompliantTerms() => new()
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

    #endregion
}
