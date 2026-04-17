using Encina.Compliance.Retention.Model;
using Encina.Compliance.Retention.ReadModels;

using Shouldly;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for read model default property values and edge cases
/// not covered by existing model tests.
/// </summary>
public sealed class ReadModelDefaultsTests
{
    #region LegalHoldReadModel

    [Fact]
    public void LegalHoldReadModel_DefaultValues_AreCorrect()
    {
        var model = new LegalHoldReadModel();

        model.Id.ShouldBe(Guid.Empty);
        model.EntityId.ShouldBe(string.Empty);
        model.Reason.ShouldBe(string.Empty);
        model.AppliedByUserId.ShouldBe(string.Empty);
        model.IsActive.ShouldBeFalse();
        model.ReleasedByUserId.ShouldBeNull();
        model.ReleasedAtUtc.ShouldBeNull();
        model.TenantId.ShouldBeNull();
        model.ModuleId.ShouldBeNull();
        model.Version.ShouldBe(0);
    }

    [Fact]
    public void LegalHoldReadModel_SetProperties_Roundtrip()
    {
        var id = Guid.NewGuid();
        var appliedAt = DateTimeOffset.UtcNow;

        var model = new LegalHoldReadModel
        {
            Id = id,
            EntityId = "cust-123",
            Reason = "Litigation",
            AppliedByUserId = "legal-counsel",
            IsActive = true,
            AppliedAtUtc = appliedAt,
            TenantId = "tenant-1",
            ModuleId = "mod-a",
            LastModifiedAtUtc = appliedAt,
            Version = 1
        };

        model.Id.ShouldBe(id);
        model.EntityId.ShouldBe("cust-123");
        model.IsActive.ShouldBeTrue();
        model.TenantId.ShouldBe("tenant-1");
        model.ModuleId.ShouldBe("mod-a");
    }

    #endregion

    #region RetentionPolicyReadModel

    [Fact]
    public void RetentionPolicyReadModel_DefaultValues_AreCorrect()
    {
        var model = new RetentionPolicyReadModel();

        model.Id.ShouldBe(Guid.Empty);
        model.DataCategory.ShouldBe(string.Empty);
        model.RetentionPeriod.ShouldBe(TimeSpan.Zero);
        model.AutoDelete.ShouldBeFalse();
        model.PolicyType.ShouldBe(RetentionPolicyType.TimeBased);
        model.Reason.ShouldBeNull();
        model.LegalBasis.ShouldBeNull();
        model.IsActive.ShouldBeFalse();
        model.DeactivationReason.ShouldBeNull();
        model.TenantId.ShouldBeNull();
        model.ModuleId.ShouldBeNull();
        model.Version.ShouldBe(0);
    }

    [Fact]
    public void RetentionPolicyReadModel_SetProperties_Roundtrip()
    {
        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var model = new RetentionPolicyReadModel
        {
            Id = id,
            DataCategory = "financial-records",
            RetentionPeriod = TimeSpan.FromDays(2555),
            AutoDelete = true,
            PolicyType = RetentionPolicyType.EventBased,
            Reason = "Tax law",
            LegalBasis = "Tax Code section 147",
            IsActive = true,
            TenantId = "tenant-2",
            ModuleId = "mod-b",
            CreatedAtUtc = createdAt,
            LastModifiedAtUtc = createdAt,
            Version = 3
        };

        model.DataCategory.ShouldBe("financial-records");
        model.AutoDelete.ShouldBeTrue();
        model.PolicyType.ShouldBe(RetentionPolicyType.EventBased);
        model.LegalBasis.ShouldBe("Tax Code section 147");
        model.Version.ShouldBe(3);
    }

    #endregion

    #region RetentionRecordReadModel — IsExpired edge cases

    [Fact]
    public void RetentionRecordReadModel_IsExpired_WhenUnderLegalHold_ReturnsTrue()
    {
        // UnderLegalHold is not RetentionStatus.Deleted, so IsExpired should return true
        // if past expiration date
        var model = new RetentionRecordReadModel
        {
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(-5),
            Status = RetentionStatus.UnderLegalHold
        };

        model.IsExpired(DateTimeOffset.UtcNow).ShouldBeTrue();
    }

    [Fact]
    public void RetentionRecordReadModel_IsExpired_WhenExpiredStatus_AndPastExpiration_ReturnsTrue()
    {
        var model = new RetentionRecordReadModel
        {
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(-10),
            Status = RetentionStatus.Expired
        };

        model.IsExpired(DateTimeOffset.UtcNow).ShouldBeTrue();
    }

    #endregion
}
