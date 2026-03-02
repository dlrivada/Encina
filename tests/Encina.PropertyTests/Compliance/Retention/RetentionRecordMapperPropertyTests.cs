using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.Retention;

/// <summary>
/// Property-based tests for <see cref="RetentionRecordMapper"/>, <see cref="RetentionPolicyMapper"/>,
/// and <see cref="LegalHoldMapper"/> verifying roundtrip invariants between domain models
/// and persistence entities.
/// </summary>
public class RetentionRecordMapperPropertyTests
{
    #region RetentionRecordMapper Roundtrip Invariants

    /// <summary>
    /// Invariant: ToEntity then ToDomain always returns an equivalent RetentionRecord
    /// for all valid RetentionStatus values.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property ToEntity_ToDomain_Roundtrip_PreservesAllFields()
    {
        var statusGen = Gen.Elements(
            RetentionStatus.Active,
            RetentionStatus.Expired,
            RetentionStatus.Deleted,
            RetentionStatus.UnderLegalHold);

        return Prop.ForAll(
            Arb.From(statusGen),
            status =>
            {
                var record = new RetentionRecord
                {
                    Id = Guid.NewGuid().ToString("N"),
                    EntityId = $"entity-{Guid.NewGuid():N}",
                    DataCategory = "financial-records",
                    PolicyId = null,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(30),
                    Status = status,
                    DeletedAtUtc = null,
                    LegalHoldId = null
                };

                var entity = RetentionRecordMapper.ToEntity(record);
                var roundtripped = RetentionRecordMapper.ToDomain(entity);

                roundtripped.ShouldNotBeNull();
                roundtripped!.Id.ShouldBe(record.Id);
                roundtripped.EntityId.ShouldBe(record.EntityId);
                roundtripped.DataCategory.ShouldBe(record.DataCategory);
                roundtripped.Status.ShouldBe(record.Status);
                roundtripped.CreatedAtUtc.ShouldBe(record.CreatedAtUtc);
                roundtripped.ExpiresAtUtc.ShouldBe(record.ExpiresAtUtc);
                roundtripped.DeletedAtUtc.ShouldBeNull();
                roundtripped.LegalHoldId.ShouldBeNull();
            });
    }

    /// <summary>
    /// Invariant: ToEntity then ToDomain preserves optional PolicyId when provided.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool ToEntity_ToDomain_Roundtrip_PreservesPolicyId(
        NonEmptyString entityId,
        NonEmptyString category,
        NonEmptyString policyId)
    {
        var record = new RetentionRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            EntityId = entityId.Get,
            DataCategory = category.Get,
            PolicyId = policyId.Get,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(30),
            Status = RetentionStatus.Active
        };

        var entity = RetentionRecordMapper.ToEntity(record);
        var roundtripped = RetentionRecordMapper.ToDomain(entity);

        return roundtripped is not null
            && roundtripped.PolicyId == policyId.Get;
    }

    /// <summary>
    /// Invariant: ToEntity then ToDomain preserves optional LegalHoldId when provided.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool ToEntity_ToDomain_Roundtrip_PreservesLegalHoldId(
        NonEmptyString entityId,
        NonEmptyString category,
        NonEmptyString holdId)
    {
        var record = new RetentionRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            EntityId = entityId.Get,
            DataCategory = category.Get,
            PolicyId = null,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(30),
            Status = RetentionStatus.UnderLegalHold,
            LegalHoldId = holdId.Get
        };

        var entity = RetentionRecordMapper.ToEntity(record);
        var roundtripped = RetentionRecordMapper.ToDomain(entity);

        return roundtripped is not null
            && roundtripped.LegalHoldId == holdId.Get;
    }

    /// <summary>
    /// Invariant: ToDomain returns null for an entity with an invalid StatusValue (not a defined enum).
    /// </summary>
    [Property(MaxTest = 50)]
    public Property ToDomain_InvalidStatusValue_ReturnsNull()
    {
        // StatusValues 0-3 are valid; use values outside that range
        return Prop.ForAll(
            Gen.Choose(100, 10000).ToArbitrary(),
            invalidStatus =>
            {
                var entity = new RetentionRecordEntity
                {
                    Id = Guid.NewGuid().ToString("N"),
                    EntityId = "entity-1",
                    DataCategory = "cat",
                    StatusValue = invalidStatus,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(30)
                };

                var result = RetentionRecordMapper.ToDomain(entity);
                result.ShouldBeNull();
            });
    }

    #endregion

    #region RetentionPolicyMapper Roundtrip Invariants

    /// <summary>
    /// Invariant: RetentionPolicyMapper ToEntity then ToDomain preserves all fields
    /// for all valid RetentionPolicyType values.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property PolicyMapper_ToEntity_ToDomain_Roundtrip_PreservesAllFields()
    {
        var policyTypeGen = Gen.Elements(
            RetentionPolicyType.TimeBased,
            RetentionPolicyType.EventBased,
            RetentionPolicyType.ConsentBased);

        return Prop.ForAll(
            Arb.From(policyTypeGen),
            policyType =>
            {
                var now = DateTimeOffset.UtcNow;

                var policy = new RetentionPolicy
                {
                    Id = Guid.NewGuid().ToString("N"),
                    DataCategory = $"cat-{Guid.NewGuid():N}",
                    RetentionPeriod = TimeSpan.FromDays(365 * 7),
                    AutoDelete = true,
                    Reason = "Tax law",
                    LegalBasis = "Art. 6(1)(c)",
                    PolicyType = policyType,
                    CreatedAtUtc = now,
                    LastModifiedAtUtc = null
                };

                var entity = RetentionPolicyMapper.ToEntity(policy);
                var roundtripped = RetentionPolicyMapper.ToDomain(entity);

                roundtripped.ShouldNotBeNull();
                roundtripped!.Id.ShouldBe(policy.Id);
                roundtripped.DataCategory.ShouldBe(policy.DataCategory);
                roundtripped.RetentionPeriod.ShouldBe(policy.RetentionPeriod);
                roundtripped.AutoDelete.ShouldBe(policy.AutoDelete);
                roundtripped.Reason.ShouldBe(policy.Reason);
                roundtripped.LegalBasis.ShouldBe(policy.LegalBasis);
                roundtripped.PolicyType.ShouldBe(policy.PolicyType);
                roundtripped.CreatedAtUtc.ShouldBe(policy.CreatedAtUtc);
                roundtripped.LastModifiedAtUtc.ShouldBeNull();
            });
    }

    /// <summary>
    /// Invariant: RetentionPeriod is preserved losslessly through ticks-based storage
    /// for any valid positive TimeSpan.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool PolicyMapper_RetentionPeriod_PreservedLosslesslyViaTicks(
        NonEmptyString category,
        PositiveInt days)
    {
        var period = TimeSpan.FromDays(days.Get);

        var policy = RetentionPolicy.Create(
            dataCategory: category.Get,
            retentionPeriod: period);

        var entity = RetentionPolicyMapper.ToEntity(policy);
        var roundtripped = RetentionPolicyMapper.ToDomain(entity);

        return roundtripped is not null
            && roundtripped.RetentionPeriod == period;
    }

    /// <summary>
    /// Invariant: RetentionPolicyMapper ToDomain returns null for invalid PolicyTypeValue.
    /// </summary>
    [Property(MaxTest = 50)]
    public Property PolicyMapper_ToDomain_InvalidPolicyType_ReturnsNull()
    {
        return Prop.ForAll(
            Gen.Choose(100, 10000).ToArbitrary(),
            invalidType =>
            {
                var entity = new RetentionPolicyEntity
                {
                    Id = Guid.NewGuid().ToString("N"),
                    DataCategory = "cat",
                    RetentionPeriodTicks = TimeSpan.FromDays(30).Ticks,
                    AutoDelete = true,
                    PolicyTypeValue = invalidType,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                };

                var result = RetentionPolicyMapper.ToDomain(entity);
                result.ShouldBeNull();
            });
    }

    #endregion

    #region LegalHoldMapper Roundtrip Invariants

    /// <summary>
    /// Invariant: LegalHoldMapper ToEntity then ToDomain preserves all fields.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool LegalHoldMapper_ToEntity_ToDomain_Roundtrip_PreservesAllFields(
        NonEmptyString entityId,
        NonEmptyString reason)
    {
        var hold = LegalHold.Create(
            entityId: entityId.Get,
            reason: reason.Get,
            appliedByUserId: "user-123");

        var entity = LegalHoldMapper.ToEntity(hold);
        var roundtripped = LegalHoldMapper.ToDomain(entity);

        return roundtripped.Id == hold.Id
            && roundtripped.EntityId == hold.EntityId
            && roundtripped.Reason == hold.Reason
            && roundtripped.AppliedByUserId == hold.AppliedByUserId
            && roundtripped.AppliedAtUtc == hold.AppliedAtUtc
            && roundtripped.ReleasedAtUtc == hold.ReleasedAtUtc
            && roundtripped.ReleasedByUserId == hold.ReleasedByUserId;
    }

    /// <summary>
    /// Invariant: LegalHoldMapper roundtrip preserves IsActive for an active hold.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool LegalHoldMapper_Roundtrip_PreservesIsActiveForActiveHold(
        NonEmptyString entityId,
        NonEmptyString reason)
    {
        var hold = LegalHold.Create(
            entityId: entityId.Get,
            reason: reason.Get);

        var entity = LegalHoldMapper.ToEntity(hold);
        var roundtripped = LegalHoldMapper.ToDomain(entity);

        return roundtripped.IsActive == hold.IsActive && roundtripped.IsActive;
    }

    /// <summary>
    /// Invariant: LegalHoldMapper roundtrip preserves IsActive for a released hold
    /// (ReleasedAtUtc set means IsActive is false).
    /// </summary>
    [Property(MaxTest = 50)]
    public bool LegalHoldMapper_Roundtrip_PreservesIsActiveForReleasedHold(
        NonEmptyString entityId,
        NonEmptyString reason)
    {
        var hold = LegalHold.Create(
            entityId: entityId.Get,
            reason: reason.Get);

        var releasedHold = hold with
        {
            ReleasedAtUtc = DateTimeOffset.UtcNow,
            ReleasedByUserId = "release-user"
        };

        var entity = LegalHoldMapper.ToEntity(releasedHold);
        var roundtripped = LegalHoldMapper.ToDomain(entity);

        return roundtripped.IsActive == releasedHold.IsActive
            && !roundtripped.IsActive
            && roundtripped.ReleasedAtUtc == releasedHold.ReleasedAtUtc
            && roundtripped.ReleasedByUserId == releasedHold.ReleasedByUserId;
    }

    /// <summary>
    /// Invariant: LegalHoldMapper entity -> domain -> entity roundtrip preserves all primitive fields.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool LegalHoldMapper_ToDomain_ToEntity_Roundtrip_PreservesAllFields(
        NonEmptyString entityId,
        NonEmptyString reason)
    {
        var entity = new LegalHoldEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            EntityId = entityId.Get,
            Reason = reason.Get,
            AppliedByUserId = "user-applied",
            AppliedAtUtc = DateTimeOffset.UtcNow,
            ReleasedAtUtc = null,
            ReleasedByUserId = null
        };

        var domain = LegalHoldMapper.ToDomain(entity);
        var roundtripped = LegalHoldMapper.ToEntity(domain);

        return roundtripped.Id == entity.Id
            && roundtripped.EntityId == entity.EntityId
            && roundtripped.Reason == entity.Reason
            && roundtripped.AppliedByUserId == entity.AppliedByUserId
            && roundtripped.AppliedAtUtc == entity.AppliedAtUtc
            && roundtripped.ReleasedAtUtc == entity.ReleasedAtUtc
            && roundtripped.ReleasedByUserId == entity.ReleasedByUserId;
    }

    #endregion
}
