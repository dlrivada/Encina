using System.Collections.Concurrent;

using Encina.Caching;
using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Abstractions;
using Encina.Compliance.Retention.Aggregates;
using Encina.Compliance.Retention.Events;
using Encina.Compliance.Retention.Model;
using Encina.Compliance.Retention.ReadModels;
using Encina.DomainModeling;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten;
using Encina.Testing.Fakes.Providers;

using LanguageExt;

using Marten;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Compliance.Retention;

/// <summary>
/// End-to-end enforcement cycle against Marten on PostgreSQL (#1142, #1143, #1146): a tracked record
/// whose retention period has elapsed goes <c>Active → Expired → Deleted</c>, its entity is erased
/// exactly once, and later cycles leave it alone; a held record is not erased until its hold is released.
/// </summary>
/// <remarks>
/// The store is shared by the whole Marten collection, so every assertion is scoped to the entity
/// identifiers this class creates.
/// </remarks>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class RetentionEnforcementIntegrationTests : IAsyncLifetime
{
    private readonly MartenFixture _fixture;
    private readonly FakeTimeProvider _timeProvider = new(DateTimeOffset.UtcNow);
    private readonly RecordingDataEraser _dataEraser = new();

    public RetentionEnforcementIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "Marten PostgreSQL container not available");
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSingleton(_fixture.Store!);
        services.AddScoped(sp => sp.GetRequiredService<IDocumentStore>().LightweightSession());
        services.AddEncinaMarten();

        // Registered before AddEncinaRetention, whose TryAdd keeps this fake clock.
        services.AddSingleton<TimeProvider>(_timeProvider);
        services.AddEncinaRetention(options =>
        {
            options.EnableAutomaticEnforcement = false; // cycles are driven explicitly below
            options.PublishNotifications = false;
        });
        services.AddRetentionAggregates();

        services.AddSingleton<ICacheProvider>(new FakeCacheProvider());
        services.AddSingleton<IRetentionDataEraser>(_dataEraser);

        var requestContext = Substitute.For<IRequestContext>();
        requestContext.CorrelationId.Returns(Guid.NewGuid().ToString());
        services.AddScoped<IRequestContext>(_ => requestContext);

        return services.BuildServiceProvider();
    }

    private RetentionEnforcementService CreateEnforcementService(IServiceProvider provider) =>
        new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IOptions<RetentionOptions>>(),
            NullLogger<RetentionEnforcementService>.Instance,
            _timeProvider);

    private static async Task<Guid> TrackAsync(
        IServiceProvider provider,
        string entityId,
        TimeSpan retentionPeriod,
        string dataCategory = "enforcement-it",
        string? tenantId = null)
    {
        using var scope = provider.CreateScope();
        var recordService = scope.ServiceProvider.GetRequiredService<IRetentionRecordService>();
        var tracked = await recordService.TrackEntityAsync(
            entityId, dataCategory, Guid.NewGuid(), retentionPeriod, tenantId: tenantId);
        return tracked.Match(id => id, error => throw new InvalidOperationException($"Track failed: {error.Message}"));
    }

    private static async Task<Guid> PlaceHoldAsync(IServiceProvider provider, string entityId, string reason)
    {
        using var scope = provider.CreateScope();
        var legalHoldService = scope.ServiceProvider.GetRequiredService<ILegalHoldService>();
        var placed = await legalHoldService.PlaceHoldAsync(entityId, reason, "legal-officer");
        return placed.Match(id => id, error => throw new InvalidOperationException($"PlaceHold failed: {error.Message}"));
    }

    private static async Task<Either<EncinaError, Unit>> LiftHoldAsync(IServiceProvider provider, Guid holdId)
    {
        using var scope = provider.CreateScope();
        var legalHoldService = scope.ServiceProvider.GetRequiredService<ILegalHoldService>();
        return await legalHoldService.LiftHoldAsync(holdId, "legal-officer");
    }

    private async Task<(RetentionStatus AggregateStatus, RetentionStatus ReadModelStatus)> LoadStatusAsync(
        IServiceProvider provider, Guid recordId)
    {
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAggregateRepository<RetentionRecordAggregate>>();
        var aggregate = (await repository.LoadAsync(recordId))
            .Match(a => a, error => throw new InvalidOperationException($"Load failed: {error.Message}"));

        await using var session = _fixture.Store!.LightweightSession();
        var readModel = await session.LoadAsync<RetentionRecordReadModel>(recordId);
        readModel.ShouldNotBeNull();

        return (aggregate.Status, readModel.Status);
    }

    [Fact]
    public async Task Cycle_ExpiredRecord_ReachesDeleted_AndNextCycleDoesNothing()
    {
        await using var provider = BuildServiceProvider();
        var entityId = $"customer-{Guid.NewGuid():N}";
        var recordId = await TrackAsync(provider, entityId, TimeSpan.FromDays(1));
        var sut = CreateEnforcementService(provider);

        // Not expired yet: the cycle must not touch it
        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);
        _dataEraser.CallsFor(entityId).ShouldBe(0);
        (await LoadStatusAsync(provider, recordId)).ShouldBe((RetentionStatus.Active, RetentionStatus.Active));

        // Past the retention period: Active → Expired → Deleted, erased once
        _timeProvider.Advance(TimeSpan.FromDays(2));
        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);

        _dataEraser.CallsFor(entityId).ShouldBe(1);
        (await LoadStatusAsync(provider, recordId)).ShouldBe((RetentionStatus.Deleted, RetentionStatus.Deleted));

        // Next cycle: the deleted record is not selected and the entity is not erased again
        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);

        _dataEraser.CallsFor(entityId).ShouldBe(1);
        (await LoadStatusAsync(provider, recordId)).ShouldBe((RetentionStatus.Deleted, RetentionStatus.Deleted));
    }

    [Fact]
    public async Task Cycle_ErasureFails_RecordStaysExpired_AndIsDeletedOnTheNextCycle()
    {
        await using var provider = BuildServiceProvider();
        var entityId = $"patient-{Guid.NewGuid():N}";
        _dataEraser.FailNextCallFor(entityId);
        var recordId = await TrackAsync(provider, entityId, TimeSpan.FromDays(1));
        var sut = CreateEnforcementService(provider);
        _timeProvider.Advance(TimeSpan.FromDays(2));

        // First cycle: marked expired, erasure fails, not marked deleted
        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);

        _dataEraser.CallsFor(entityId).ShouldBe(1);
        (await LoadStatusAsync(provider, recordId)).ShouldBe((RetentionStatus.Expired, RetentionStatus.Expired));

        // Second cycle: the expired record is retried and completes
        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);

        _dataEraser.CallsFor(entityId).ShouldBe(2);
        (await LoadStatusAsync(provider, recordId)).ShouldBe((RetentionStatus.Deleted, RetentionStatus.Deleted));
    }

    [Fact]
    public async Task Cycle_HeldRecord_IsNotErased_AndAfterReleaseGoesExpiredThenDeleted()
    {
        await using var provider = BuildServiceProvider();
        var entityId = $"litigant-{Guid.NewGuid():N}";
        var recordId = await TrackAsync(provider, entityId, TimeSpan.FromDays(1));
        var sut = CreateEnforcementService(provider);

        Guid holdId;
        using (var scope = provider.CreateScope())
        {
            var legalHoldService = scope.ServiceProvider.GetRequiredService<ILegalHoldService>();
            var placed = await legalHoldService.PlaceHoldAsync(entityId, "Pending litigation", "legal-officer");
            holdId = placed.Match(id => id, error => throw new InvalidOperationException($"PlaceHold failed: {error.Message}"));
        }

        (await LoadStatusAsync(provider, recordId)).ShouldBe((RetentionStatus.UnderLegalHold, RetentionStatus.UnderLegalHold));

        // Past the retention period but held: nothing is erased and the record stays held
        _timeProvider.Advance(TimeSpan.FromDays(2));
        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);

        _dataEraser.CallsFor(entityId).ShouldBe(0);
        (await LoadStatusAsync(provider, recordId)).ShouldBe((RetentionStatus.UnderLegalHold, RetentionStatus.UnderLegalHold));

        // Releasing the hold after the expiry returns the record to Expired, not Active
        using (var scope = provider.CreateScope())
        {
            var legalHoldService = scope.ServiceProvider.GetRequiredService<ILegalHoldService>();
            var lifted = await legalHoldService.LiftHoldAsync(holdId, "legal-officer");
            lifted.IsRight.ShouldBeTrue();
        }

        (await LoadStatusAsync(provider, recordId)).ShouldBe((RetentionStatus.Expired, RetentionStatus.Expired));

        // Next cycle: the released record is erased once and reaches Deleted
        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);

        _dataEraser.CallsFor(entityId).ShouldBe(1);
        (await LoadStatusAsync(provider, recordId)).ShouldBe((RetentionStatus.Deleted, RetentionStatus.Deleted));
    }

    [Fact]
    public async Task Cycle_EntityWithTwoCategories_ErasesOnlyTheExpiredCategory()
    {
        // #1160: a patient's contact data is kept 1 year and the clinical record 5 years. When the
        // contact record expires, only the contact data is erased; the clinical data stays.
        await using var provider = BuildServiceProvider();
        var entityId = $"patient-{Guid.NewGuid():N}";
        _dataEraser.Store(entityId, "patient-contact");
        _dataEraser.Store(entityId, "clinical-record");
        var contactRecordId = await TrackAsync(provider, entityId, TimeSpan.FromDays(365), "patient-contact");
        var clinicalRecordId = await TrackAsync(provider, entityId, TimeSpan.FromDays(5 * 365), "clinical-record");
        var sut = CreateEnforcementService(provider);

        _timeProvider.Advance(TimeSpan.FromDays(366));
        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);

        _dataEraser.Holds(entityId, "patient-contact").ShouldBeFalse();
        _dataEraser.Holds(entityId, "clinical-record").ShouldBeTrue();
        _dataEraser.CategoriesErasedFor(entityId).ShouldBe(["patient-contact"]);
        (await LoadStatusAsync(provider, contactRecordId)).ShouldBe((RetentionStatus.Deleted, RetentionStatus.Deleted));
        (await LoadStatusAsync(provider, clinicalRecordId)).ShouldBe((RetentionStatus.Active, RetentionStatus.Active));

        // Four years later the clinical record expires too and only then is its data erased.
        _timeProvider.Advance(TimeSpan.FromDays(4 * 365));
        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);

        _dataEraser.Holds(entityId, "clinical-record").ShouldBeFalse();
        _dataEraser.CategoriesErasedFor(entityId).ShouldBe(["patient-contact", "clinical-record"]);
        (await LoadStatusAsync(provider, clinicalRecordId)).ShouldBe((RetentionStatus.Deleted, RetentionStatus.Deleted));
    }

    [Fact]
    public async Task Cycle_TwoEpisodesOfTheSameCategory_ErasesNothingUntilTheLastExpires_ThenErasesOnce()
    {
        // #1185 review: one patient, two clinical episodes tracked in the same category. Episode 1
        // expires in year 5, episode 2 in year 8. Erasing the category in year 5 would destroy episode
        // 2's data, which must be kept until year 8.
        await using var provider = BuildServiceProvider();
        var entityId = $"patient-{Guid.NewGuid():N}";
        _dataEraser.Store(entityId, "clinical-record");
        var episode1 = await TrackAsync(provider, entityId, TimeSpan.FromDays(5 * 365), "clinical-record", "clinic-1");
        var episode2 = await TrackAsync(provider, entityId, TimeSpan.FromDays(8 * 365), "clinical-record", "clinic-1");
        var sut = CreateEnforcementService(provider);

        // Year 5: episode 1 expires but episode 2 still retains the category — nothing is erased,
        // episode 1 stays Expired (no DataDeleted event), episode 2 stays Active.
        _timeProvider.Advance(TimeSpan.FromDays(5 * 365 + 1));
        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);

        _dataEraser.CallsFor(entityId).ShouldBe(0);
        _dataEraser.Holds(entityId, "clinical-record").ShouldBeTrue();
        (await LoadStatusAsync(provider, episode1)).ShouldBe((RetentionStatus.Expired, RetentionStatus.Expired));
        (await LoadStatusAsync(provider, episode2)).ShouldBe((RetentionStatus.Active, RetentionStatus.Active));

        // Year 6: still deferred.
        _timeProvider.Advance(TimeSpan.FromDays(365));
        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);
        _dataEraser.CallsFor(entityId).ShouldBe(0);

        // Year 8: episode 2 expires — the category is erased once and both records reach Deleted.
        _timeProvider.Advance(TimeSpan.FromDays(2 * 365));
        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);

        _dataEraser.CallsFor(entityId).ShouldBe(1);
        _dataEraser.Holds(entityId, "clinical-record").ShouldBeFalse();
        (await LoadStatusAsync(provider, episode1)).ShouldBe((RetentionStatus.Deleted, RetentionStatus.Deleted));
        (await LoadStatusAsync(provider, episode2)).ShouldBe((RetentionStatus.Deleted, RetentionStatus.Deleted));

        // Later cycles erase nothing more.
        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);
        _dataEraser.CallsFor(entityId).ShouldBe(1);

        // The audit trail is honest: exactly one DataDeleted event per record, none before year 8.
        await using var session = _fixture.Store!.LightweightSession();
        (await session.Events.FetchStreamAsync(episode1)).Count(e => e.Data is DataDeleted).ShouldBe(1);
        (await session.Events.FetchStreamAsync(episode2)).Count(e => e.Data is DataDeleted).ShouldBe(1);
    }

    [Fact]
    public async Task Cycle_SameEntityAndCategoryInAnotherTenant_DoesNotDeferErasure()
    {
        // Entity identifiers are only unique within a tenant: a retained record of another tenant does
        // not hold back the erasure of this tenant's expired record.
        await using var provider = BuildServiceProvider();
        var entityId = $"patient-{Guid.NewGuid():N}";
        var tenantA = await TrackAsync(provider, entityId, TimeSpan.FromDays(1), "clinical-record", "clinic-a");
        var tenantB = await TrackAsync(provider, entityId, TimeSpan.FromDays(5 * 365), "clinical-record", "clinic-b");
        var sut = CreateEnforcementService(provider);

        _timeProvider.Advance(TimeSpan.FromDays(2));
        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);

        _dataEraser.CallsFor(entityId).ShouldBe(1);
        _dataEraser.TenantsErasedFor(entityId).ShouldBe(["clinic-a"]);
        (await LoadStatusAsync(provider, tenantA)).ShouldBe((RetentionStatus.Deleted, RetentionStatus.Deleted));
        (await LoadStatusAsync(provider, tenantB)).ShouldBe((RetentionStatus.Active, RetentionStatus.Active));
    }

    [Fact]
    public async Task LiftHold_WhileAnotherHoldRemains_KeepsRecordHeld_UntilTheLastHoldIsLifted()
    {
        // #1161: lifting one of two holds must not release the entity's records.
        await using var provider = BuildServiceProvider();
        var entityId = $"litigant-{Guid.NewGuid():N}";
        var recordId = await TrackAsync(provider, entityId, TimeSpan.FromDays(1));
        var sut = CreateEnforcementService(provider);
        var firstHold = await PlaceHoldAsync(provider, entityId, "Case A");
        var secondHold = await PlaceHoldAsync(provider, entityId, "Case B");
        _timeProvider.Advance(TimeSpan.FromDays(2));

        (await LiftHoldAsync(provider, firstHold)).IsRight.ShouldBeTrue();

        (await LoadStatusAsync(provider, recordId)).ShouldBe((RetentionStatus.UnderLegalHold, RetentionStatus.UnderLegalHold));
        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);
        _dataEraser.CallsFor(entityId).ShouldBe(0);

        (await LiftHoldAsync(provider, secondHold)).IsRight.ShouldBeTrue();

        (await LoadStatusAsync(provider, recordId)).ShouldBe((RetentionStatus.Expired, RetentionStatus.Expired));
        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);
        _dataEraser.CallsFor(entityId).ShouldBe(1);
        (await LoadStatusAsync(provider, recordId)).ShouldBe((RetentionStatus.Deleted, RetentionStatus.Deleted));
    }

    [Fact]
    public async Task LiftHold_CalledAgainAfterItWasLifted_IsIdempotent_AndDoesNotReleaseTwice()
    {
        // #1161: a retry of LiftHoldAsync on an already lifted hold succeeds without a second lift
        // event, and the released record is not touched again.
        await using var provider = BuildServiceProvider();
        var entityId = $"litigant-{Guid.NewGuid():N}";
        var recordId = await TrackAsync(provider, entityId, TimeSpan.FromDays(1));
        var holdId = await PlaceHoldAsync(provider, entityId, "Case C");
        _timeProvider.Advance(TimeSpan.FromDays(2));

        (await LiftHoldAsync(provider, holdId)).IsRight.ShouldBeTrue();
        (await LiftHoldAsync(provider, holdId)).IsRight.ShouldBeTrue();

        (await LoadStatusAsync(provider, recordId)).ShouldBe((RetentionStatus.Expired, RetentionStatus.Expired));
        await using var session = _fixture.Store!.LightweightSession();
        var holdEvents = await session.Events.FetchStreamAsync(holdId);
        holdEvents.Count(e => e.Data is LegalHoldLifted).ShouldBe(1);

        // A release requested from a stale read model (the record is no longer held) succeeds without
        // writing a second release event.
        using (var scope = provider.CreateScope())
        {
            var recordService = scope.ServiceProvider.GetRequiredService<IRetentionRecordService>();
            (await recordService.ReleaseRecordAsync(recordId, holdId)).IsRight.ShouldBeTrue();
        }

        var recordEvents = await session.Events.FetchStreamAsync(recordId);
        recordEvents.Count(e => e.Data is RetentionRecordReleased released && released.LegalHoldId == holdId).ShouldBe(1);
    }

    /// <summary>
    /// Data eraser backed by an in-memory set of (entity, category) pairs: it erases exactly the pair it
    /// is asked for, records every call and can fail on demand.
    /// </summary>
    private sealed class RecordingDataEraser : IRetentionDataEraser
    {
        private readonly ConcurrentDictionary<string, int> _calls = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, bool> _failNext = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<(string EntityId, string DataCategory), bool> _data = new();
        private readonly ConcurrentQueue<(string EntityId, string DataCategory)> _erased = new();
        private readonly ConcurrentQueue<(string EntityId, string? TenantId)> _erasedTenants = new();

        public string?[] TenantsErasedFor(string entityId) =>
            [.. _erasedTenants.Where(e => e.EntityId == entityId).Select(e => e.TenantId)];

        public int CallsFor(string entityId) => _calls.TryGetValue(entityId, out var count) ? count : 0;

        public void FailNextCallFor(string entityId) => _failNext[entityId] = true;

        public void Store(string entityId, string dataCategory) => _data[(entityId, dataCategory)] = true;

        public bool Holds(string entityId, string dataCategory) => _data.ContainsKey((entityId, dataCategory));

        public string[] CategoriesErasedFor(string entityId) =>
            [.. _erased.Where(e => e.EntityId == entityId).Select(e => e.DataCategory)];

        public ValueTask<Either<EncinaError, Unit>> EraseAsync(
            RetentionErasureTarget target,
            CancellationToken cancellationToken = default)
        {
            _calls.AddOrUpdate(target.EntityId, 1, static (_, count) => count + 1);

            if (_failNext.TryRemove(target.EntityId, out _))
            {
                return ValueTask.FromResult(Left<EncinaError, Unit>(
                    EncinaError.New("Simulated erasure failure")));
            }

            _data.TryRemove((target.EntityId, target.DataCategory), out _);
            _erased.Enqueue((target.EntityId, target.DataCategory));
            _erasedTenants.Enqueue((target.EntityId, target.TenantId));
            return ValueTask.FromResult(Right<EncinaError, Unit>(unit));
        }
    }
}
