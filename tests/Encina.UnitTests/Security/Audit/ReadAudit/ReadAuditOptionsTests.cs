using Encina.Security.Audit;
using FluentAssertions;

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
        options.Enabled.Should().BeTrue();
        options.ExcludeSystemAccess.Should().BeFalse();
        options.RequirePurpose.Should().BeFalse();
        options.BatchSize.Should().Be(1);
        options.RetentionDays.Should().Be(365);
        options.EnableAutoPurge.Should().BeFalse();
        options.PurgeIntervalHours.Should().Be(24);
        options.AuditedEntityTypes.Should().BeEmpty();
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
        options.AuditedEntityTypes.Should().ContainKey(typeof(TestEntity));
        options.AuditedEntityTypes[typeof(TestEntity)].Should().Be(1.0);
    }

    [Fact]
    public void AuditReadsFor_WithRate_ShouldRegisterWithSpecifiedRate()
    {
        // Arrange
        var options = new ReadAuditOptions();

        // Act
        options.AuditReadsFor<TestEntity>(0.1);

        // Assert
        options.GetSamplingRate(typeof(TestEntity)).Should().Be(0.1);
    }

    [Fact]
    public void AuditReadsFor_WithRate_ShouldClampAboveOne()
    {
        // Arrange
        var options = new ReadAuditOptions();

        // Act
        options.AuditReadsFor<TestEntity>(1.5);

        // Assert
        options.GetSamplingRate(typeof(TestEntity)).Should().Be(1.0);
    }

    [Fact]
    public void AuditReadsFor_WithRate_ShouldClampBelowZero()
    {
        // Arrange
        var options = new ReadAuditOptions();

        // Act
        options.AuditReadsFor<TestEntity>(-0.5);

        // Assert
        options.GetSamplingRate(typeof(TestEntity)).Should().Be(0.0);
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
        result.Should().BeSameAs(options);
        options.AuditedEntityTypes.Should().HaveCount(2);
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
        options.GetSamplingRate(typeof(TestEntity)).Should().Be(0.9);
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
        options.IsAuditable(typeof(TestEntity)).Should().BeTrue();
    }

    [Fact]
    public void IsAuditable_UnregisteredType_ShouldReturnFalse()
    {
        // Arrange
        var options = new ReadAuditOptions();

        // Act & Assert
        options.IsAuditable(typeof(TestEntity)).Should().BeFalse();
    }

    [Fact]
    public void IsAuditable_WhenDisabled_ShouldReturnFalse()
    {
        // Arrange
        var options = new ReadAuditOptions { Enabled = false };
        options.AuditReadsFor<TestEntity>();

        // Act & Assert
        options.IsAuditable(typeof(TestEntity)).Should().BeFalse();
    }

    [Fact]
    public void IsAuditable_NullType_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new ReadAuditOptions();

        // Act
        var act = () => options.IsAuditable(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("entityType");
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
        options.GetSamplingRate(typeof(TestEntity)).Should().Be(0.25);
    }

    [Fact]
    public void GetSamplingRate_UnregisteredType_ShouldReturnZero()
    {
        // Arrange
        var options = new ReadAuditOptions();

        // Act & Assert
        options.GetSamplingRate(typeof(TestEntity)).Should().Be(0.0);
    }

    [Fact]
    public void GetSamplingRate_WhenDisabled_ShouldReturnZero()
    {
        // Arrange
        var options = new ReadAuditOptions { Enabled = false };
        options.AuditReadsFor<TestEntity>();

        // Act & Assert
        options.GetSamplingRate(typeof(TestEntity)).Should().Be(0.0);
    }

    [Fact]
    public void GetSamplingRate_NullType_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new ReadAuditOptions();

        // Act
        var act = () => options.GetSamplingRate(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("entityType");
    }

    #endregion

    #region Test Entities

    private sealed class TestEntity;
    private sealed class AnotherTestEntity;

    #endregion
}
