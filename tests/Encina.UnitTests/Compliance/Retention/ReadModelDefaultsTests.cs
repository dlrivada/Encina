using Encina.Compliance.Retention.Model;
using Encina.Compliance.Retention.ReadModels;

using FluentAssertions;

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

        model.Id.Should().Be(Guid.Empty);
        model.EntityId.Should().Be(string.Empty);
        model.Reason.Should().Be(string.Empty);
        model.AppliedByUserId.Should().Be(string.Empty);
        model.IsActive.Should().BeFalse();
        model.ReleasedByUserId.Should().BeNull();
        model.ReleasedAtUtc.Should().BeNull();
        model.TenantId.Should().BeNull();
        model.ModuleId.Should().BeNull();
        model.Version.Should().Be(0);
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

        model.Id.Should().Be(id);
        model.EntityId.Should().Be("cust-123");
        model.IsActive.Should().BeTrue();
        model.TenantId.Should().Be("tenant-1");
        model.ModuleId.Should().Be("mod-a");
    }

    #endregion

    #region RetentionPolicyReadModel

    [Fact]
    public void RetentionPolicyReadModel_DefaultValues_AreCorrect()
    {
        var model = new RetentionPolicyReadModel();

        model.Id.Should().Be(Guid.Empty);
        model.DataCategory.Should().Be(string.Empty);
        model.RetentionPeriod.Should().Be(TimeSpan.Zero);
        model.AutoDelete.Should().BeFalse();
        model.PolicyType.Should().Be(RetentionPolicyType.TimeBased);
        model.Reason.Should().BeNull();
        model.LegalBasis.Should().BeNull();
        model.IsActive.Should().BeFalse();
        model.DeactivationReason.Should().BeNull();
        model.TenantId.Should().BeNull();
        model.ModuleId.Should().BeNull();
        model.Version.Should().Be(0);
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

        model.DataCategory.Should().Be("financial-records");
        model.AutoDelete.Should().BeTrue();
        model.PolicyType.Should().Be(RetentionPolicyType.EventBased);
        model.LegalBasis.Should().Be("Tax Code section 147");
        model.Version.Should().Be(3);
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

        model.IsExpired(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void RetentionRecordReadModel_IsExpired_WhenExpiredStatus_AndPastExpiration_ReturnsTrue()
    {
        var model = new RetentionRecordReadModel
        {
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(-10),
            Status = RetentionStatus.Expired
        };

        model.IsExpired(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    #endregion
}
