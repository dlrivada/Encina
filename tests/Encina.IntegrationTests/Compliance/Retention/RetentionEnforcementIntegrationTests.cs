using System.Collections.Concurrent;

using Encina.Caching;
using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Abstractions;
using Encina.Compliance.Retention.Aggregates;
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
    private readonly RecordingErasureExecutor _erasureExecutor = new();

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
        services.AddSingleton<IDataErasureExecutor>(_erasureExecutor);

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

    private static async Task<Guid> TrackAsync(IServiceProvider provider, string entityId, TimeSpan retentionPeriod)
    {
        using var scope = provider.CreateScope();
        var recordService = scope.ServiceProvider.GetRequiredService<IRetentionRecordService>();
        var tracked = await recordService.TrackEntityAsync(entityId, "enforcement-it", Guid.NewGuid(), retentionPeriod);
        return tracked.Match(id => id, error => throw new InvalidOperationException($"Track failed: {error.Message}"));
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
        _erasureExecutor.CallsFor(entityId).ShouldBe(0);
        (await LoadStatusAsync(provider, recordId)).ShouldBe((RetentionStatus.Active, RetentionStatus.Active));

        // Past the retention period: Active → Expired → Deleted, erased once
        _timeProvider.Advance(TimeSpan.FromDays(2));
        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);

        _erasureExecutor.CallsFor(entityId).ShouldBe(1);
        (await LoadStatusAsync(provider, recordId)).ShouldBe((RetentionStatus.Deleted, RetentionStatus.Deleted));

        // Next cycle: the deleted record is not selected and the entity is not erased again
        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);

        _erasureExecutor.CallsFor(entityId).ShouldBe(1);
        (await LoadStatusAsync(provider, recordId)).ShouldBe((RetentionStatus.Deleted, RetentionStatus.Deleted));
    }

    [Fact]
    public async Task Cycle_ErasureFails_RecordStaysExpired_AndIsDeletedOnTheNextCycle()
    {
        await using var provider = BuildServiceProvider();
        var entityId = $"patient-{Guid.NewGuid():N}";
        _erasureExecutor.FailNextCallFor(entityId);
        var recordId = await TrackAsync(provider, entityId, TimeSpan.FromDays(1));
        var sut = CreateEnforcementService(provider);
        _timeProvider.Advance(TimeSpan.FromDays(2));

        // First cycle: marked expired, erasure fails, not marked deleted
        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);

        _erasureExecutor.CallsFor(entityId).ShouldBe(1);
        (await LoadStatusAsync(provider, recordId)).ShouldBe((RetentionStatus.Expired, RetentionStatus.Expired));

        // Second cycle: the expired record is retried and completes
        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);

        _erasureExecutor.CallsFor(entityId).ShouldBe(2);
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

        _erasureExecutor.CallsFor(entityId).ShouldBe(0);
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

        _erasureExecutor.CallsFor(entityId).ShouldBe(1);
        (await LoadStatusAsync(provider, recordId)).ShouldBe((RetentionStatus.Deleted, RetentionStatus.Deleted));
    }

    /// <summary>
    /// Erasure executor that records how many times each entity was erased and can fail on demand.
    /// </summary>
    private sealed class RecordingErasureExecutor : IDataErasureExecutor
    {
        private readonly ConcurrentDictionary<string, int> _calls = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, bool> _failNext = new(StringComparer.Ordinal);

        public int CallsFor(string entityId) => _calls.TryGetValue(entityId, out var count) ? count : 0;

        public void FailNextCallFor(string entityId) => _failNext[entityId] = true;

        public ValueTask<Either<EncinaError, ErasureResult>> EraseAsync(
            string subjectId,
            ErasureScope scope,
            CancellationToken cancellationToken = default)
        {
            _calls.AddOrUpdate(subjectId, 1, static (_, count) => count + 1);

            if (_failNext.TryRemove(subjectId, out _))
            {
                return ValueTask.FromResult(Left<EncinaError, ErasureResult>(
                    EncinaError.New("Simulated erasure failure")));
            }

            return ValueTask.FromResult(Right<EncinaError, ErasureResult>(new ErasureResult
            {
                FieldsErased = 1,
                FieldsRetained = 0,
                FieldsFailed = 0,
                RetentionReasons = [],
                Exemptions = []
            }));
        }
    }
}
