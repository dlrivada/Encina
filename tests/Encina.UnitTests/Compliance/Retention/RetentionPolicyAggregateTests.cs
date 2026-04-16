using Encina.Compliance.Retention.Aggregates;
using Encina.Compliance.Retention.Events;
using Encina.Compliance.Retention.Model;
using Shouldly;

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
        policy.Id.ShouldBe(id);
        policy.DataCategory.ShouldBe(dataCategory);
        policy.RetentionPeriod.ShouldBe(retentionPeriod);
        policy.AutoDelete.ShouldBeTrue();
        policy.PolicyType.ShouldBe(policyType);
        policy.Reason.ShouldBe(reason);
        policy.LegalBasis.ShouldBe(legalBasis);
        policy.IsActive.ShouldBeTrue();
        policy.TenantId.ShouldBe(tenantId);
        policy.ModuleId.ShouldBe(moduleId);
        policy.CreatedAtUtc.ShouldBe(Now);
        policy.LastUpdatedAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void Create_ValidParameters_ShouldRaiseRetentionPolicyCreatedEvent()
    {
        // Act
        var policy = CreateActivePolicy();

        // Assert
        policy.UncommittedEvents.Count.ShouldBe(1);
        policy.UncommittedEvents[0].ShouldBeOfType<RetentionPolicyCreated>();
    }

    [Fact]
    public void Create_ValidParameters_IdShouldMatchUncommittedEvent()
    {
        // Act
        var policy = CreateActivePolicy();

        // Assert
        var evt = policy.UncommittedEvents[0].ShouldBeOfType<RetentionPolicyCreated>().Subject;
        evt.PolicyId.ShouldBe(policy.Id);
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
        Should.Throw<ArgumentException>(act);
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
        Should.Throw<ArgumentOutOfRangeException>(act);
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
        policy.RetentionPeriod.ShouldBe(newPeriod);
        policy.AutoDelete.ShouldBeFalse();
        policy.Reason.ShouldBe(newReason);
        policy.LegalBasis.ShouldBe(newLegalBasis);
        policy.LastUpdatedAtUtc.ShouldBe(updateTime);
    }

    [Fact]
    public void Update_ActivePolicy_ShouldRaiseRetentionPolicyUpdatedEvent()
    {
        // Arrange
        var policy = CreateActivePolicy();

        // Act
        policy.Update(TimeSpan.FromDays(730), false, "Updated reason", "New basis", Now.AddDays(30));

        // Assert
        policy.UncommittedEvents.Count.ShouldBe(2);
        policy.UncommittedEvents[1].ShouldBeOfType<RetentionPolicyUpdated>();
    }

    [Fact]
    public void Update_DeactivatedPolicy_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var policy = CreateDeactivatedPolicy();

        // Act
        var act = () => policy.Update(TimeSpan.FromDays(730), false, "reason", "basis", Now.AddDays(2));

        // Assert
        Should.Throw<InvalidOperationException>(act);
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
        Should.Throw<ArgumentOutOfRangeException>(act);
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
        policy.IsActive.ShouldBeFalse();
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
        policy.LastUpdatedAtUtc.ShouldBe(deactivateTime);
    }

    [Fact]
    public void Deactivate_ActivePolicy_ShouldRaiseRetentionPolicyDeactivatedEvent()
    {
        // Arrange
        var policy = CreateActivePolicy();

        // Act
        policy.Deactivate("No longer needed", Now.AddDays(1));

        // Assert
        policy.UncommittedEvents.Count.ShouldBe(2);
        policy.UncommittedEvents[1].ShouldBeOfType<RetentionPolicyDeactivated>();
    }

    [Fact]
    public void Deactivate_AlreadyDeactivated_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var policy = CreateDeactivatedPolicy();

        // Act
        var act = () => policy.Deactivate("Again", Now.AddDays(2));

        // Assert
        Should.Throw<InvalidOperationException>(act);
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
        Should.Throw<ArgumentException>(act);
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
