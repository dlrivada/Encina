using Encina.Compliance.Retention.InMemory;
using Encina.Compliance.Retention.Model;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Encina.PropertyTests.Compliance.Retention;

/// <summary>
/// Property-based tests for <see cref="InMemoryRetentionRecordStore"/> verifying store
/// invariants using FsCheck random data generation.
/// </summary>
public class InMemoryRetentionRecordStorePropertyTests
{
    private static InMemoryRetentionRecordStore CreateStore() =>
        new(new FakeTimeProvider(DateTimeOffset.UtcNow),
            NullLogger<InMemoryRetentionRecordStore>.Instance);

    #region Store Roundtrip Invariants

    /// <summary>
    /// Invariant: Any stored record can always be retrieved by its Id.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Store_ThenGetById_AlwaysReturnsStoredRecord(
        NonEmptyString entityId,
        NonEmptyString category)
    {
        var store = CreateStore();

        var record = RetentionRecord.Create(
            entityId: entityId.Get,
            dataCategory: category.Get,
            createdAtUtc: DateTimeOffset.UtcNow,
            expiresAtUtc: DateTimeOffset.UtcNow.AddDays(30));

        var createResult = store.CreateAsync(record).AsTask().Result;
        if (!createResult.IsRight) return false;

        var result = store.GetByIdAsync(record.Id).AsTask().Result;
        if (!result.IsRight) return false;

        var option = result.Match(Right: r => r, Left: _ => Option<RetentionRecord>.None);
        return option.Match(
            Some: retrieved => retrieved.Id == record.Id
                && retrieved.EntityId == record.EntityId
                && retrieved.DataCategory == record.DataCategory,
            None: () => false);
    }

    /// <summary>
    /// Invariant: After creating a record, GetByEntityId always includes that record.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Store_ThenGetByEntityId_AlwaysIncludesStoredRecord(
        NonEmptyString entityId,
        NonEmptyString category)
    {
        var store = CreateStore();

        var record = RetentionRecord.Create(
            entityId: entityId.Get,
            dataCategory: category.Get,
            createdAtUtc: DateTimeOffset.UtcNow,
            expiresAtUtc: DateTimeOffset.UtcNow.AddDays(30));

        var createResult = store.CreateAsync(record).AsTask().Result;
        if (!createResult.IsRight) return false;

        var result = store.GetByEntityIdAsync(entityId.Get).AsTask().Result;
        if (!result.IsRight) return false;

        var records = result.Match(
            Right: list => list,
            Left: _ => (IReadOnlyList<RetentionRecord>)Array.Empty<RetentionRecord>());

        return records.Any(r => r.Id == record.Id);
    }

    /// <summary>
    /// Invariant: After creating N records with unique Ids, Count equals N.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property Store_CreateNRecords_CountEqualsN()
    {
        return Prop.ForAll(
            Gen.Choose(1, 15).ToArbitrary(),
            count =>
            {
                var store = CreateStore();

                for (var i = 0; i < count; i++)
                {
                    var record = RetentionRecord.Create(
                        entityId: $"entity-{Guid.NewGuid():N}",
                        dataCategory: $"category-{i}",
                        createdAtUtc: DateTimeOffset.UtcNow,
                        expiresAtUtc: DateTimeOffset.UtcNow.AddDays(30));

                    store.CreateAsync(record).AsTask().Result
                        .IsRight.ShouldBeTrue();
                }

                store.Count.ShouldBe(count);
            });
    }

    /// <summary>
    /// Invariant: GetAllAsync after N creates returns exactly N records.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property Store_GetAll_ReturnsExactlyNRecords()
    {
        return Prop.ForAll(
            Gen.Choose(1, 10).ToArbitrary(),
            count =>
            {
                var store = CreateStore();

                for (var i = 0; i < count; i++)
                {
                    var record = RetentionRecord.Create(
                        entityId: $"entity-{Guid.NewGuid():N}",
                        dataCategory: $"cat-{i}",
                        createdAtUtc: DateTimeOffset.UtcNow,
                        expiresAtUtc: DateTimeOffset.UtcNow.AddDays(30));

                    store.CreateAsync(record).AsTask().Result
                        .IsRight.ShouldBeTrue();
                }

                var allResult = store.GetAllAsync().AsTask().Result;
                allResult.IsRight.ShouldBeTrue();

                var all = allResult.Match(
                    Right: list => list,
                    Left: _ => (IReadOnlyList<RetentionRecord>)Array.Empty<RetentionRecord>());

                all.Count.ShouldBe(count);
            });
    }

    #endregion

    #region UpdateStatus Invariants

    /// <summary>
    /// Invariant: UpdateStatus then GetById always reflects the new status
    /// for Active, Expired, and UnderLegalHold transitions.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property UpdateStatus_ThenGetById_ReflectsNewStatus()
    {
        // Pick a status index 0-2 (skip Deleted to avoid timestamp side-effects)
        var statusIndexGen = Gen.Choose(0, 2).Select(i => i switch
        {
            0 => RetentionStatus.Active,
            1 => RetentionStatus.Expired,
            _ => RetentionStatus.UnderLegalHold
        });

        return Prop.ForAll(
            Arb.From(statusIndexGen),
            newStatus =>
            {
                var store = CreateStore();

                var record = RetentionRecord.Create(
                    entityId: $"entity-{Guid.NewGuid():N}",
                    dataCategory: "cat",
                    createdAtUtc: DateTimeOffset.UtcNow,
                    expiresAtUtc: DateTimeOffset.UtcNow.AddDays(30));

                store.CreateAsync(record).AsTask().Result
                    .IsRight.ShouldBeTrue();

                store.UpdateStatusAsync(record.Id, newStatus).AsTask().Result
                    .IsRight.ShouldBeTrue();

                var result = store.GetByIdAsync(record.Id).AsTask().Result;
                result.IsRight.ShouldBeTrue();

                var option = result.Match(Right: r => r, Left: _ => Option<RetentionRecord>.None);
                option.IsSome.ShouldBeTrue();

                var updated = option.Match(Some: r => r, None: () => null!);
                updated.Status.ShouldBe(newStatus);
            });
    }

    /// <summary>
    /// Invariant: When status is updated to Deleted, DeletedAtUtc is set and non-null.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool UpdateStatus_ToDeleted_SetsDeletedAtUtc(
        NonEmptyString entityId,
        NonEmptyString category)
    {
        var store = CreateStore();

        var record = RetentionRecord.Create(
            entityId: entityId.Get,
            dataCategory: category.Get,
            createdAtUtc: DateTimeOffset.UtcNow,
            expiresAtUtc: DateTimeOffset.UtcNow.AddDays(30));

        var createResult = store.CreateAsync(record).AsTask().Result;
        if (!createResult.IsRight) return false;

        var updateResult = store.UpdateStatusAsync(record.Id, RetentionStatus.Deleted).AsTask().Result;
        if (!updateResult.IsRight) return false;

        var result = store.GetByIdAsync(record.Id).AsTask().Result;
        if (!result.IsRight) return false;

        var option = result.Match(Right: r => r, Left: _ => Option<RetentionRecord>.None);
        return option.Match(Some: r => r.DeletedAtUtc is not null, None: () => false);
    }

    #endregion

    #region Clear Invariants

    /// <summary>
    /// Invariant: After Clear, Count is always 0.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property Clear_ThenCount_IsZero()
    {
        return Prop.ForAll(
            Gen.Choose(1, 10).ToArbitrary(),
            count =>
            {
                var store = CreateStore();

                for (var i = 0; i < count; i++)
                {
                    var record = RetentionRecord.Create(
                        entityId: $"entity-{Guid.NewGuid():N}",
                        dataCategory: $"cat-{i}",
                        createdAtUtc: DateTimeOffset.UtcNow,
                        expiresAtUtc: DateTimeOffset.UtcNow.AddDays(30));

                    store.CreateAsync(record).AsTask().Result
                        .IsRight.ShouldBeTrue();
                }

                store.Count.ShouldBe(count);

                store.Clear();

                store.Count.ShouldBe(0);
            });
    }

    /// <summary>
    /// Invariant: After Clear, GetAllAsync returns an empty list.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property Clear_ThenGetAll_ReturnsEmpty()
    {
        return Prop.ForAll(
            Gen.Choose(1, 10).ToArbitrary(),
            count =>
            {
                var store = CreateStore();

                for (var i = 0; i < count; i++)
                {
                    var record = RetentionRecord.Create(
                        entityId: $"entity-{Guid.NewGuid():N}",
                        dataCategory: $"cat-{i}",
                        createdAtUtc: DateTimeOffset.UtcNow,
                        expiresAtUtc: DateTimeOffset.UtcNow.AddDays(30));

                    store.CreateAsync(record).AsTask().Result
                        .IsRight.ShouldBeTrue();
                }

                store.Clear();

                var allResult = store.GetAllAsync().AsTask().Result;
                allResult.IsRight.ShouldBeTrue();

                var all = allResult.Match(
                    Right: list => list,
                    Left: _ => (IReadOnlyList<RetentionRecord>)Array.Empty<RetentionRecord>());

                all.Count.ShouldBe(0);
            });
    }

    #endregion

    #region Non-existent Lookup Invariants

    /// <summary>
    /// Invariant: GetById for a non-existent Id always returns None (never an error).
    /// </summary>
    [Property(MaxTest = 50)]
    public bool GetById_NonExistent_AlwaysReturnsNone(NonEmptyString recordId)
    {
        var store = CreateStore();

        var result = store.GetByIdAsync(recordId.Get).AsTask().Result;
        if (!result.IsRight) return false;

        var option = result.Match(Right: r => r, Left: _ => Option<RetentionRecord>.None);
        return option.IsNone;
    }

    /// <summary>
    /// Invariant: GetByEntityId for a non-existent entityId always returns an empty list (never an error).
    /// </summary>
    [Property(MaxTest = 50)]
    public bool GetByEntityId_NonExistent_AlwaysReturnsEmptyList(NonEmptyString entityId)
    {
        var store = CreateStore();

        var result = store.GetByEntityIdAsync(entityId.Get).AsTask().Result;
        if (!result.IsRight) return false;

        var records = result.Match(
            Right: list => list,
            Left: _ => (IReadOnlyList<RetentionRecord>)Array.Empty<RetentionRecord>());

        return records.Count == 0;
    }

    #endregion
}
