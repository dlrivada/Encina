using Encina.Compliance.Retention.Aggregates;
using Encina.Compliance.Retention.Events;
using Encina.Compliance.Retention.Model;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="RetentionPolicyAggregate"/>.
/// </summary>
public class RetentionPolicyAggregateTests
{
    private static readonly Guid DefaultId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    #region Create Tests

    [Fact]
    public void Create_ValidParameters_ShouldSetAllProperties()
    {
        // Arrange
        var id = DefaultId;
        var dataCategory = "customer-data";
        var retentionPeriod = TimeSpan.FromDays(365);
        var autoDelete = true;
        var policyType = RetentionPolicyType.TimeBased;
        var reason = "GDPR compliance";
        var legalBasis = "Art. 5(1)(e)";
        var tenantId = "tenant-1";
        var moduleId = "module-crm";

        // Act
        var policy = RetentionPolicyAggregate.Create(
            id, dataCategory, retentionPeriod, autoDelete, policyType,
            reason, legalBasis, Now, tenantId, moduleId);

        // Assert
        policy.Id.Should().Be(id);
        policy.DataCategory.Should().Be(dataCategory);
        policy.RetentionPeriod.Should().Be(retentionPeriod);
        policy.AutoDelete.Should().BeTrue();
        policy.PolicyType.Should().Be(policyType);
        policy.Reason.Should().Be(reason);
        policy.LegalBasis.Should().Be(legalBasis);
        policy.IsActive.Should().BeTrue();
        policy.TenantId.Should().Be(tenantId);
        policy.ModuleId.Should().Be(moduleId);
        policy.CreatedAtUtc.Should().Be(Now);
        policy.LastUpdatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Create_ValidParameters_ShouldRaiseRetentionPolicyCreatedEvent()
    {
        // Act
        var policy = CreateActivePolicy();

        // Assert
        policy.UncommittedEvents.Should().HaveCount(1);
        policy.UncommittedEvents[0].Should().BeOfType<RetentionPolicyCreated>();
    }

    [Fact]
    public void Create_ValidParameters_IdShouldMatchUncommittedEvent()
    {
        // Act
        var policy = CreateActivePolicy();

        // Assert
        var evt = policy.UncommittedEvents[0].Should().BeOfType<RetentionPolicyCreated>().Subject;
        evt.PolicyId.Should().Be(policy.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NullOrWhitespaceDataCategory_ShouldThrowArgumentException(string? dataCategory)
    {
        // Act
        var act = () => RetentionPolicyAggregate.Create(
            DefaultId, dataCategory!, TimeSpan.FromDays(365), true,
            RetentionPolicyType.TimeBased, "reason", "basis", Now);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-365)]
    public void Create_ZeroOrNegativeRetentionPeriod_ShouldThrowArgumentOutOfRangeException(int days)
    {
        // Act
        var act = () => RetentionPolicyAggregate.Create(
            DefaultId, "customer-data", TimeSpan.FromDays(days), true,
            RetentionPolicyType.TimeBased, "reason", "basis", Now);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ActivePolicy_ShouldUpdateProperties()
    {
        // Arrange
        var policy = CreateActivePolicy();
        var updateTime = Now.AddDays(30);
        var newPeriod = TimeSpan.FromDays(730);
        var newReason = "Updated GDPR compliance";
        var newLegalBasis = "Tax Code §147";

        // Act
        policy.Update(newPeriod, false, newReason, newLegalBasis, updateTime);

        // Assert
        policy.RetentionPeriod.Should().Be(newPeriod);
        policy.AutoDelete.Should().BeFalse();
        policy.Reason.Should().Be(newReason);
        policy.LegalBasis.Should().Be(newLegalBasis);
        policy.LastUpdatedAtUtc.Should().Be(updateTime);
    }

    [Fact]
    public void Update_ActivePolicy_ShouldRaiseRetentionPolicyUpdatedEvent()
    {
        // Arrange
        var policy = CreateActivePolicy();

        // Act
        policy.Update(TimeSpan.FromDays(730), false, "Updated reason", "New basis", Now.AddDays(30));

        // Assert
        policy.UncommittedEvents.Should().HaveCount(2);
        policy.UncommittedEvents[1].Should().BeOfType<RetentionPolicyUpdated>();
    }

    [Fact]
    public void Update_DeactivatedPolicy_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var policy = CreateDeactivatedPolicy();

        // Act
        var act = () => policy.Update(TimeSpan.FromDays(730), false, "reason", "basis", Now.AddDays(2));

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Update_ZeroOrNegativeRetentionPeriod_ShouldThrowArgumentOutOfRangeException(int days)
    {
        // Arrange
        var policy = CreateActivePolicy();

        // Act
        var act = () => policy.Update(TimeSpan.FromDays(days), true, "reason", "basis", Now.AddDays(1));

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Deactivate Tests

    [Fact]
    public void Deactivate_ActivePolicy_ShouldSetIsActiveFalse()
    {
        // Arrange
        var policy = CreateActivePolicy();
        var deactivateTime = Now.AddDays(1);

        // Act
        policy.Deactivate("No longer needed", deactivateTime);

        // Assert
        policy.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_ActivePolicy_ShouldUpdateLastUpdatedAtUtc()
    {
        // Arrange
        var policy = CreateActivePolicy();
        var deactivateTime = Now.AddDays(1);

        // Act
        policy.Deactivate("No longer needed", deactivateTime);

        // Assert
        policy.LastUpdatedAtUtc.Should().Be(deactivateTime);
    }

    [Fact]
    public void Deactivate_ActivePolicy_ShouldRaiseRetentionPolicyDeactivatedEvent()
    {
        // Arrange
        var policy = CreateActivePolicy();

        // Act
        policy.Deactivate("No longer needed", Now.AddDays(1));

        // Assert
        policy.UncommittedEvents.Should().HaveCount(2);
        policy.UncommittedEvents[1].Should().BeOfType<RetentionPolicyDeactivated>();
    }

    [Fact]
    public void Deactivate_AlreadyDeactivated_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var policy = CreateDeactivatedPolicy();

        // Act
        var act = () => policy.Deactivate("Again", Now.AddDays(2));

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Deactivate_NullOrWhitespaceReason_ShouldThrowArgumentException(string? reason)
    {
        // Arrange
        var policy = CreateActivePolicy();

        // Act
        var act = () => policy.Deactivate(reason!, Now.AddDays(1));

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Helpers

    private static RetentionPolicyAggregate CreateActivePolicy() =>
        RetentionPolicyAggregate.Create(DefaultId, "customer-data", TimeSpan.FromDays(365), true,
            RetentionPolicyType.TimeBased, "GDPR compliance", "Art. 5(1)(e)", Now);

    private static RetentionPolicyAggregate CreateDeactivatedPolicy()
    {
        var agg = CreateActivePolicy();
        agg.Deactivate("No longer needed", Now.AddDays(1));
        return agg;
    }

    #endregion
}
