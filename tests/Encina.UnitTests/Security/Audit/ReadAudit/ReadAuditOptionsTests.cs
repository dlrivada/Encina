using Encina.Security.Audit;
using Shouldly;

namespace Encina.UnitTests.Security.Audit.ReadAudit;

/// <summary>
/// Unit tests for <see cref="ReadAuditOptions"/>.
/// </summary>
public class ReadAuditOptionsTests
{
    #region Default Values Tests

    [Fact]
    public void Defaults_ShouldHaveExpectedValues()
    {
        // Arrange & Act
        var options = new ReadAuditOptions();

        // Assert
        options.Enabled.ShouldBeTrue();
        options.ExcludeSystemAccess.ShouldBeFalse();
        options.RequirePurpose.ShouldBeFalse();
        options.BatchSize.ShouldBe(1);
        options.RetentionDays.ShouldBe(365);
        options.EnableAutoPurge.ShouldBeFalse();
        options.PurgeIntervalHours.ShouldBe(24);
        options.AuditedEntityTypes.ShouldBeEmpty();
    }

    #endregion

    #region AuditReadsFor Tests

    [Fact]
    public void AuditReadsFor_WithoutRate_ShouldRegisterWithFullSampling()
    {
        // Arrange
        var options = new ReadAuditOptions();

        // Act
        options.AuditReadsFor<TestEntity>();

        // Assert
        options.AuditedEntityTypes.ShouldContainKey(typeof(TestEntity));
        options.AuditedEntityTypes[typeof(TestEntity)].ShouldBe(1.0);
    }

    [Fact]
    public void AuditReadsFor_WithRate_ShouldRegisterWithSpecifiedRate()
    {
        // Arrange
        var options = new ReadAuditOptions();

        // Act
        options.AuditReadsFor<TestEntity>(0.1);

        // Assert
        options.GetSamplingRate(typeof(TestEntity)).ShouldBe(0.1);
    }

    [Fact]
    public void AuditReadsFor_WithRate_ShouldClampAboveOne()
    {
        // Arrange
        var options = new ReadAuditOptions();

        // Act
        options.AuditReadsFor<TestEntity>(1.5);

        // Assert
        options.GetSamplingRate(typeof(TestEntity)).ShouldBe(1.0);
    }

    [Fact]
    public void AuditReadsFor_WithRate_ShouldClampBelowZero()
    {
        // Arrange
        var options = new ReadAuditOptions();

        // Act
        options.AuditReadsFor<TestEntity>(-0.5);

        // Assert
        options.GetSamplingRate(typeof(TestEntity)).ShouldBe(0.0);
    }

    [Fact]
    public void AuditReadsFor_ShouldSupportFluentChaining()
    {
        // Arrange
        var options = new ReadAuditOptions();

        // Act
        var result = options
            .AuditReadsFor<TestEntity>()
            .AuditReadsFor<AnotherTestEntity>(0.5);

        // Assert
        result.ShouldBeSameAs(options);
        options.AuditedEntityTypes.Count.ShouldBe(2);
    }

    [Fact]
    public void AuditReadsFor_SameTypeTwice_ShouldOverwriteSamplingRate()
    {
        // Arrange
        var options = new ReadAuditOptions();

        // Act
        options.AuditReadsFor<TestEntity>(0.1);
        options.AuditReadsFor<TestEntity>(0.9);

        // Assert
        options.GetSamplingRate(typeof(TestEntity)).ShouldBe(0.9);
    }

    #endregion

    #region IsAuditable Tests

    [Fact]
    public void IsAuditable_RegisteredType_ShouldReturnTrue()
    {
        // Arrange
        var options = new ReadAuditOptions();
        options.AuditReadsFor<TestEntity>();

        // Act & Assert
        options.IsAuditable(typeof(TestEntity)).ShouldBeTrue();
    }

    [Fact]
    public void IsAuditable_UnregisteredType_ShouldReturnFalse()
    {
        // Arrange
        var options = new ReadAuditOptions();

        // Act & Assert
        options.IsAuditable(typeof(TestEntity)).ShouldBeFalse();
    }

    [Fact]
    public void IsAuditable_WhenDisabled_ShouldReturnFalse()
    {
        // Arrange
        var options = new ReadAuditOptions { Enabled = false };
        options.AuditReadsFor<TestEntity>();

        // Act & Assert
        options.IsAuditable(typeof(TestEntity)).ShouldBeFalse();
    }

    [Fact]
    public void IsAuditable_NullType_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new ReadAuditOptions();

        // Act
        Action act = () => { options.IsAuditable(null!); };

        // Assert
        Should.Throw<ArgumentNullException>(act)
                .ParamName.ShouldBe("entityType");
    }

    #endregion

    #region GetSamplingRate Tests

    [Fact]
    public void GetSamplingRate_RegisteredType_ShouldReturnRate()
    {
        // Arrange
        var options = new ReadAuditOptions();
        options.AuditReadsFor<TestEntity>(0.25);

        // Act & Assert
        options.GetSamplingRate(typeof(TestEntity)).ShouldBe(0.25);
    }

    [Fact]
    public void GetSamplingRate_UnregisteredType_ShouldReturnZero()
    {
        // Arrange
        var options = new ReadAuditOptions();

        // Act & Assert
        options.GetSamplingRate(typeof(TestEntity)).ShouldBe(0.0);
    }

    [Fact]
    public void GetSamplingRate_WhenDisabled_ShouldReturnZero()
    {
        // Arrange
        var options = new ReadAuditOptions { Enabled = false };
        options.AuditReadsFor<TestEntity>();

        // Act & Assert
        options.GetSamplingRate(typeof(TestEntity)).ShouldBe(0.0);
    }

    [Fact]
    public void GetSamplingRate_NullType_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new ReadAuditOptions();

        // Act
        Action act = () => { options.GetSamplingRate(null!); };

        // Assert
        Should.Throw<ArgumentNullException>(act)
                .ParamName.ShouldBe("entityType");
    }

    #endregion

    #region Test Entities

    private sealed class TestEntity;
    private sealed class AnotherTestEntity;

    #endregion
}
