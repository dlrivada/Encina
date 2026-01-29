using Encina.DomainModeling;

namespace Encina.UnitTests.DomainModeling;

/// <summary>
/// Tests for concurrency support (RowVersion) in AggregateRoot and its variants.
/// </summary>
public class AggregateRootConcurrencyTests
{
    private sealed class TestAggregate : AggregateRoot<Guid>
    {
        public string Name { get; set; } = string.Empty;

        public TestAggregate(Guid id) : base(id) { }
    }

    private sealed class TestAuditableAggregate : AuditableAggregateRoot<Guid>
    {
        public string Name { get; set; } = string.Empty;

        public TestAuditableAggregate(Guid id) : base(id) { }
    }

    private sealed class TestSoftDeletableAggregate : SoftDeletableAggregateRoot<Guid>
    {
        public string Name { get; set; } = string.Empty;

        public TestSoftDeletableAggregate(Guid id) : base(id) { }
    }

    #region AggregateRoot RowVersion Tests

    [Fact]
    public void AggregateRoot_RowVersion_ShouldDefaultToNull()
    {
        // Arrange & Act
        var aggregate = new TestAggregate(Guid.NewGuid());

        // Assert
        aggregate.RowVersion.ShouldBeNull();
    }

    [Fact]
    public void AggregateRoot_RowVersion_ShouldBeSettable()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());
        var rowVersion = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };

        // Act
        aggregate.RowVersion = rowVersion;

        // Assert
        aggregate.RowVersion.ShouldBe(rowVersion);
    }

    [Fact]
    public void AggregateRoot_RowVersion_ShouldAcceptDifferentVersionValues()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());
        var version1 = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };
        var version2 = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF };

        // Act & Assert
        aggregate.RowVersion = version1;
        aggregate.RowVersion.ShouldBe(version1);

        aggregate.RowVersion = version2;
        aggregate.RowVersion.ShouldBe(version2);
    }

    [Fact]
    public void AggregateRoot_RowVersion_ShouldAcceptEmptyByteArray()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());
        var emptyVersion = Array.Empty<byte>();

        // Act
        aggregate.RowVersion = emptyVersion;

        // Assert
        aggregate.RowVersion.ShouldBe(emptyVersion);
    }

    [Fact]
    public void AggregateRoot_RowVersion_CanBeResetToNull()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.RowVersion = new byte[] { 0x01, 0x02, 0x03 };

        // Act
        aggregate.RowVersion = null;

        // Assert
        aggregate.RowVersion.ShouldBeNull();
    }

    [Fact]
    public void AggregateRoot_ShouldImplementIConcurrencyAware()
    {
        // Arrange
        var aggregate = new TestAggregate(Guid.NewGuid());

        // Assert
        aggregate.ShouldBeAssignableTo<IConcurrencyAware>();
    }

    #endregion

    #region AuditableAggregateRoot RowVersion Tests

    [Fact]
    public void AuditableAggregateRoot_RowVersion_ShouldDefaultToNull()
    {
        // Arrange & Act
        var aggregate = new TestAuditableAggregate(Guid.NewGuid());

        // Assert
        aggregate.RowVersion.ShouldBeNull();
    }

    [Fact]
    public void AuditableAggregateRoot_RowVersion_ShouldBeSettable()
    {
        // Arrange
        var aggregate = new TestAuditableAggregate(Guid.NewGuid());
        var rowVersion = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };

        // Act
        aggregate.RowVersion = rowVersion;

        // Assert
        aggregate.RowVersion.ShouldBe(rowVersion);
    }

    [Fact]
    public void AuditableAggregateRoot_ShouldImplementIConcurrencyAware()
    {
        // Arrange
        var aggregate = new TestAuditableAggregate(Guid.NewGuid());

        // Assert
        aggregate.ShouldBeAssignableTo<IConcurrencyAware>();
    }

    [Fact]
    public void AuditableAggregateRoot_ShouldRetainAuditProperties_WithRowVersion()
    {
        // Arrange
        var aggregate = new TestAuditableAggregate(Guid.NewGuid());
        var rowVersion = new byte[] { 0x01, 0x02, 0x03 };

        // Act
        aggregate.SetCreatedBy("user1");
        aggregate.RowVersion = rowVersion;

        // Assert
        aggregate.CreatedBy.ShouldBe("user1");
        aggregate.RowVersion.ShouldBe(rowVersion);
    }

    #endregion

    #region SoftDeletableAggregateRoot RowVersion Tests

    [Fact]
    public void SoftDeletableAggregateRoot_RowVersion_ShouldDefaultToNull()
    {
        // Arrange & Act
        var aggregate = new TestSoftDeletableAggregate(Guid.NewGuid());

        // Assert
        aggregate.RowVersion.ShouldBeNull();
    }

    [Fact]
    public void SoftDeletableAggregateRoot_RowVersion_ShouldBeSettable()
    {
        // Arrange
        var aggregate = new TestSoftDeletableAggregate(Guid.NewGuid());
        var rowVersion = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };

        // Act
        aggregate.RowVersion = rowVersion;

        // Assert
        aggregate.RowVersion.ShouldBe(rowVersion);
    }

    [Fact]
    public void SoftDeletableAggregateRoot_ShouldImplementIConcurrencyAware()
    {
        // Arrange
        var aggregate = new TestSoftDeletableAggregate(Guid.NewGuid());

        // Assert
        aggregate.ShouldBeAssignableTo<IConcurrencyAware>();
    }

    [Fact]
    public void SoftDeletableAggregateRoot_ShouldRetainDeleteProperties_WithRowVersion()
    {
        // Arrange
        var aggregate = new TestSoftDeletableAggregate(Guid.NewGuid());
        var rowVersion = new byte[] { 0x01, 0x02, 0x03 };

        // Act
        aggregate.Delete("user1");
        aggregate.RowVersion = rowVersion;

        // Assert
        aggregate.IsDeleted.ShouldBeTrue();
        aggregate.DeletedBy.ShouldBe("user1");
        aggregate.RowVersion.ShouldBe(rowVersion);
    }

    #endregion

    #region Cross-Aggregate Variant Tests

    [Fact]
    public void AllAggregateVariants_ShouldHaveRowVersionProperty()
    {
        // Arrange
        var baseAggregate = new TestAggregate(Guid.NewGuid());
        var auditableAggregate = new TestAuditableAggregate(Guid.NewGuid());
        var softDeletableAggregate = new TestSoftDeletableAggregate(Guid.NewGuid());

        var rowVersion = new byte[] { 0xAB, 0xCD, 0xEF };

        // Act
        baseAggregate.RowVersion = rowVersion;
        auditableAggregate.RowVersion = rowVersion;
        softDeletableAggregate.RowVersion = rowVersion;

        // Assert
        baseAggregate.RowVersion.ShouldBe(rowVersion);
        auditableAggregate.RowVersion.ShouldBe(rowVersion);
        softDeletableAggregate.RowVersion.ShouldBe(rowVersion);
    }

    [Fact]
    public void AllAggregateVariants_RowVersionProperty_ShouldBeIndependent()
    {
        // Arrange
        var aggregate1 = new TestAggregate(Guid.NewGuid());
        var aggregate2 = new TestAggregate(Guid.NewGuid());

        var version1 = new byte[] { 0x01 };
        var version2 = new byte[] { 0x02 };

        // Act
        aggregate1.RowVersion = version1;
        aggregate2.RowVersion = version2;

        // Assert
        aggregate1.RowVersion.ShouldBe(version1);
        aggregate2.RowVersion.ShouldBe(version2);
        aggregate1.RowVersion.ShouldNotBe(aggregate2.RowVersion);
    }

    #endregion

    #region RowVersion in Equality Tests

    [Fact]
    public void AggregateRoot_Equality_ShouldNotConsiderRowVersion()
    {
        // Arrange
        var id = Guid.NewGuid();
        var aggregate1 = new TestAggregate(id) { RowVersion = new byte[] { 0x01 } };
        var aggregate2 = new TestAggregate(id) { RowVersion = new byte[] { 0x02 } };

        // Act & Assert - Aggregates should be equal because they have the same Id
        // RowVersion should not affect equality
        aggregate1.ShouldBe(aggregate2);
        (aggregate1 == aggregate2).ShouldBeTrue();
    }

    [Fact]
    public void AggregateRoot_Equality_WithNullAndNonNullRowVersion_ShouldStillBeEqualById()
    {
        // Arrange
        var id = Guid.NewGuid();
        var aggregate1 = new TestAggregate(id) { RowVersion = null };
        var aggregate2 = new TestAggregate(id) { RowVersion = new byte[] { 0x01, 0x02 } };

        // Act & Assert
        aggregate1.ShouldBe(aggregate2);
    }

    #endregion

    #region Typical SQL Server RowVersion Format Tests

    [Fact]
    public void AggregateRoot_RowVersion_ShouldHandle8ByteSqlServerTimestamp()
    {
        // Arrange - SQL Server timestamp/rowversion is 8 bytes
        var aggregate = new TestAggregate(Guid.NewGuid());
        var sqlServerRowVersion = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x07, 0xD5 };

        // Act
        aggregate.RowVersion = sqlServerRowVersion;

        // Assert
        aggregate.RowVersion.ShouldBe(sqlServerRowVersion);
        aggregate.RowVersion!.Length.ShouldBe(8);
    }

    [Fact]
    public void AggregateRoot_RowVersion_ShouldHandlePostgreSqlXmin()
    {
        // Arrange - PostgreSQL xmin is typically 4 bytes (uint32)
        var aggregate = new TestAggregate(Guid.NewGuid());
        var pgXmin = new byte[] { 0x00, 0x00, 0x01, 0x00 };

        // Act
        aggregate.RowVersion = pgXmin;

        // Assert
        aggregate.RowVersion.ShouldBe(pgXmin);
        aggregate.RowVersion!.Length.ShouldBe(4);
    }

    #endregion
}
