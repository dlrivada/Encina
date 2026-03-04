using Encina.Security.Audit;
using Encina.Testing.FsCheck;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Shouldly;

namespace Encina.PropertyTests.Security.Audit.ReadAudit;

/// <summary>
/// Property-based tests for <see cref="ReadAuditQuery"/> and <see cref="InMemoryReadAuditStore"/> invariants.
/// Tests that core read audit invariants hold for randomly generated inputs.
/// </summary>
public sealed class ReadAuditQueryPropertyTests : PropertyTestBase
{
    #region InMemoryReadAuditStore Invariants

    [EncinaProperty]
    public Property LogRead_ThenGetAll_AlwaysReturnsWhatWasAdded()
    {
        var idGen = Arb.From(Gen.Fresh(() => Guid.NewGuid()));
        var entityTypeGen = Arb.From(Gen.Elements("Patient", "Order", "FinancialRecord", "Customer"));

        return Prop.ForAll(idGen, entityTypeGen, (id, entityType) =>
        {
            var store = new InMemoryReadAuditStore();
            var entry = new ReadAuditEntry
            {
                Id = id,
                EntityType = entityType,
                EntityId = "E-1",
                AccessedAtUtc = DateTimeOffset.UtcNow,
                AccessMethod = ReadAccessMethod.Repository,
                EntityCount = 1
            };

            store.LogReadAsync(entry).AsTask().GetAwaiter().GetResult();

            var all = store.GetAllEntries();
            return all.Count == 1 && all[0].Id == id && all[0].EntityType == entityType;
        });
    }

    [EncinaProperty]
    public Property PurgeEntries_NeverRemovesNewerEntries()
    {
        var daysOldGen = Arb.From(Gen.Choose(1, 100));

        return Prop.ForAll(daysOldGen, daysOld =>
        {
            var store = new InMemoryReadAuditStore();
            var now = DateTimeOffset.UtcNow;

            // Add an old entry and a fresh entry
            var oldEntry = new ReadAuditEntry
            {
                Id = Guid.NewGuid(),
                EntityType = "Patient",
                EntityId = "P-1",
                AccessedAtUtc = now.AddDays(-daysOld - 1),
                AccessMethod = ReadAccessMethod.Repository,
                EntityCount = 1
            };
            var freshEntry = new ReadAuditEntry
            {
                Id = Guid.NewGuid(),
                EntityType = "Patient",
                EntityId = "P-2",
                AccessedAtUtc = now,
                AccessMethod = ReadAccessMethod.Repository,
                EntityCount = 1
            };

            store.LogReadAsync(oldEntry).AsTask().GetAwaiter().GetResult();
            store.LogReadAsync(freshEntry).AsTask().GetAwaiter().GetResult();

            var cutoff = now.AddDays(-daysOld);
            store.PurgeEntriesAsync(cutoff).AsTask().GetAwaiter().GetResult();

            // The fresh entry should never be purged
            return store.Count == 1 && store.GetAllEntries()[0].Id == freshEntry.Id;
        });
    }

    [EncinaProperty]
    public Property QueryAsync_PaginationInvariant_TotalCountNeverChanges()
    {
        var countGen = Arb.From(Gen.Choose(1, 50));
        var pageSizeGen = Arb.From(Gen.Choose(1, 20));

        return Prop.ForAll(countGen, pageSizeGen, (count, pageSize) =>
        {
            var store = new InMemoryReadAuditStore();

            for (var i = 0; i < count; i++)
            {
                store.LogReadAsync(new ReadAuditEntry
                {
                    Id = Guid.NewGuid(),
                    EntityType = "Patient",
                    EntityId = $"P-{i}",
                    AccessedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-i),
                    AccessMethod = ReadAccessMethod.Repository,
                    EntityCount = 1
                }).AsTask().GetAwaiter().GetResult();
            }

            var query1 = ReadAuditQuery.Builder().OnPage(1).WithPageSize(pageSize).Build();
            var query2 = ReadAuditQuery.Builder().OnPage(2).WithPageSize(pageSize).Build();

            var result1 = store.QueryAsync(query1).AsTask().GetAwaiter().GetResult();
            var result2 = store.QueryAsync(query2).AsTask().GetAwaiter().GetResult();

            // TotalCount should be consistent across pages
            var total1 = result1.Match(Right: p => p.TotalCount, Left: _ => -1);
            var total2 = result2.Match(Right: p => p.TotalCount, Left: _ => -1);

            return total1 == total2 && total1 == count;
        });
    }

    #endregion

    #region ReadAuditOptions Invariants

    [EncinaProperty]
    public Property SamplingRate_AlwaysClampedBetweenZeroAndOne()
    {
        var rateGen = Arb.From(Gen.Choose(-100, 200).Select(i => i / 100.0));

        return Prop.ForAll(rateGen, rate =>
        {
            var options = new ReadAuditOptions();
            options.AuditReadsFor<TestEntity>(rate);

            var actual = options.GetSamplingRate(typeof(TestEntity));
            return actual >= 0.0 && actual <= 1.0;
        });
    }

    [EncinaProperty]
    public Property IsAuditable_WhenDisabled_AlwaysFalse()
    {
        var enabledGen = Arb.From(Gen.Elements(true, false));

        return Prop.ForAll(enabledGen, enabled =>
        {
            var options = new ReadAuditOptions { Enabled = enabled };
            options.AuditReadsFor<TestEntity>();

            var isAuditable = options.IsAuditable(typeof(TestEntity));

            // When disabled, should always be false; when enabled, should be true (registered)
            return isAuditable == enabled;
        });
    }

    [EncinaProperty]
    public Property GetSamplingRate_UnregisteredType_AlwaysZero()
    {
        var enabledGen = Arb.From(Gen.Elements(true, false));

        return Prop.ForAll(enabledGen, enabled =>
        {
            var options = new ReadAuditOptions { Enabled = enabled };
            // Do NOT register TestEntity
            return options.GetSamplingRate(typeof(TestEntity)) == 0.0;
        });
    }

    #endregion

    #region ReadAuditQueryBuilder Invariants

    [EncinaProperty]
    public Property Builder_AlwaysProducesValidQuery()
    {
        var pageGen = Arb.From(Gen.Choose(1, 100));
        var pageSizeGen = Arb.From(Gen.Choose(1, 500));

        return Prop.ForAll(pageGen, pageSizeGen, (page, pageSize) =>
        {
            var query = ReadAuditQuery.Builder()
                .OnPage(page)
                .WithPageSize(pageSize)
                .Build();

            return query.PageNumber == page && query.PageSize == pageSize;
        });
    }

    #endregion

    #region Test Types

    private sealed class TestEntity;

    #endregion
}
