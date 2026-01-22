using Encina.DomainModeling;

namespace Encina.UnitTests.DomainModeling.BulkOperations;

/// <summary>
/// Unit tests for <see cref="BulkConfig"/>.
/// </summary>
[Trait("Category", "Unit")]
public class BulkConfigTests
{
    #region Default Configuration Tests

    [Fact]
    public void Default_ReturnsSingletonInstance()
    {
        // Act
        var config1 = BulkConfig.Default;
        var config2 = BulkConfig.Default;

        // Assert
        config1.ShouldBe(config2);
        ReferenceEquals(config1, config2).ShouldBeTrue();
    }

    [Fact]
    public void Default_HasExpectedBatchSize()
    {
        // Act
        var config = BulkConfig.Default;

        // Assert
        config.BatchSize.ShouldBe(2000);
    }

    [Fact]
    public void Default_HasNullBulkCopyTimeout()
    {
        // Act
        var config = BulkConfig.Default;

        // Assert
        config.BulkCopyTimeout.ShouldBeNull();
    }

    [Fact]
    public void Default_HasSetOutputIdentityDisabled()
    {
        // Act
        var config = BulkConfig.Default;

        // Assert
        config.SetOutputIdentity.ShouldBeFalse();
    }

    [Fact]
    public void Default_HasPreserveInsertOrderEnabled()
    {
        // Act
        var config = BulkConfig.Default;

        // Assert
        config.PreserveInsertOrder.ShouldBeTrue();
    }

    [Fact]
    public void Default_HasUseTempDBDisabled()
    {
        // Act
        var config = BulkConfig.Default;

        // Assert
        config.UseTempDB.ShouldBeFalse();
    }

    [Fact]
    public void Default_HasTrackingEntitiesDisabled()
    {
        // Act
        var config = BulkConfig.Default;

        // Assert
        config.TrackingEntities.ShouldBeFalse();
    }

    [Fact]
    public void Default_HasNullPropertiesToInclude()
    {
        // Act
        var config = BulkConfig.Default;

        // Assert
        config.PropertiesToInclude.ShouldBeNull();
    }

    [Fact]
    public void Default_HasNullPropertiesToExclude()
    {
        // Act
        var config = BulkConfig.Default;

        // Assert
        config.PropertiesToExclude.ShouldBeNull();
    }

    #endregion

    #region Immutability Tests (with expressions)

    [Fact]
    public void WithExpression_ChangingBatchSize_ReturnsNewInstance()
    {
        // Arrange
        var original = BulkConfig.Default;

        // Act
        var modified = original with { BatchSize = 5000 };

        // Assert
        modified.BatchSize.ShouldBe(5000);
        original.BatchSize.ShouldBe(2000); // Original unchanged
        modified.ShouldNotBe(original);
    }

    [Fact]
    public void WithExpression_ChangingBulkCopyTimeout_ReturnsNewInstance()
    {
        // Arrange
        var original = BulkConfig.Default;

        // Act
        var modified = original with { BulkCopyTimeout = 300 };

        // Assert
        modified.BulkCopyTimeout.ShouldBe(300);
        original.BulkCopyTimeout.ShouldBeNull(); // Original unchanged
    }

    [Fact]
    public void WithExpression_ChangingSetOutputIdentity_ReturnsNewInstance()
    {
        // Arrange
        var original = BulkConfig.Default;

        // Act
        var modified = original with { SetOutputIdentity = true };

        // Assert
        modified.SetOutputIdentity.ShouldBeTrue();
        original.SetOutputIdentity.ShouldBeFalse(); // Original unchanged
    }

    [Fact]
    public void WithExpression_ChangingPreserveInsertOrder_ReturnsNewInstance()
    {
        // Arrange
        var original = BulkConfig.Default;

        // Act
        var modified = original with { PreserveInsertOrder = false };

        // Assert
        modified.PreserveInsertOrder.ShouldBeFalse();
        original.PreserveInsertOrder.ShouldBeTrue(); // Original unchanged
    }

    [Fact]
    public void WithExpression_ChangingUseTempDB_ReturnsNewInstance()
    {
        // Arrange
        var original = BulkConfig.Default;

        // Act
        var modified = original with { UseTempDB = true };

        // Assert
        modified.UseTempDB.ShouldBeTrue();
        original.UseTempDB.ShouldBeFalse(); // Original unchanged
    }

    [Fact]
    public void WithExpression_ChangingTrackingEntities_ReturnsNewInstance()
    {
        // Arrange
        var original = BulkConfig.Default;

        // Act
        var modified = original with { TrackingEntities = true };

        // Assert
        modified.TrackingEntities.ShouldBeTrue();
        original.TrackingEntities.ShouldBeFalse(); // Original unchanged
    }

    [Fact]
    public void WithExpression_ChangingPropertiesToInclude_ReturnsNewInstance()
    {
        // Arrange
        var original = BulkConfig.Default;
        var properties = new[] { "Status", "UpdatedAt" };

        // Act
        var modified = original with { PropertiesToInclude = properties };

        // Assert
        modified.PropertiesToInclude.ShouldNotBeNull();
        modified.PropertiesToInclude.ShouldBe(properties);
        original.PropertiesToInclude.ShouldBeNull(); // Original unchanged
    }

    [Fact]
    public void WithExpression_ChangingPropertiesToExclude_ReturnsNewInstance()
    {
        // Arrange
        var original = BulkConfig.Default;
        var properties = new[] { "CreatedAt", "CreatedBy" };

        // Act
        var modified = original with { PropertiesToExclude = properties };

        // Assert
        modified.PropertiesToExclude.ShouldNotBeNull();
        modified.PropertiesToExclude.ShouldBe(properties);
        original.PropertiesToExclude.ShouldBeNull(); // Original unchanged
    }

    [Fact]
    public void WithExpression_ChangingMultipleProperties_ReturnsNewInstance()
    {
        // Arrange
        var original = BulkConfig.Default;

        // Act
        var modified = original with
        {
            BatchSize = 1000,
            BulkCopyTimeout = 60,
            SetOutputIdentity = true,
            PreserveInsertOrder = false
        };

        // Assert
        modified.BatchSize.ShouldBe(1000);
        modified.BulkCopyTimeout.ShouldBe(60);
        modified.SetOutputIdentity.ShouldBeTrue();
        modified.PreserveInsertOrder.ShouldBeFalse();

        // Original unchanged
        original.BatchSize.ShouldBe(2000);
        original.BulkCopyTimeout.ShouldBeNull();
        original.SetOutputIdentity.ShouldBeFalse();
        original.PreserveInsertOrder.ShouldBeTrue();
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_TwoDefaultConfigs_AreEqual()
    {
        // Arrange
        var config1 = new BulkConfig();
        var config2 = new BulkConfig();

        // Assert
        config1.ShouldBe(config2);
        (config1 == config2).ShouldBeTrue();
        (config1 != config2).ShouldBeFalse();
        config1.GetHashCode().ShouldBe(config2.GetHashCode());
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange
        var config1 = BulkConfig.Default with { BatchSize = 500 };
        var config2 = BulkConfig.Default with { BatchSize = 500 };

        // Assert
        config1.ShouldBe(config2);
        (config1 == config2).ShouldBeTrue();
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var config1 = BulkConfig.Default with { BatchSize = 500 };
        var config2 = BulkConfig.Default with { BatchSize = 1000 };

        // Assert
        config1.ShouldNotBe(config2);
        (config1 == config2).ShouldBeFalse();
        (config1 != config2).ShouldBeTrue();
    }

    [Fact]
    public void Equality_DifferentPropertiesToInclude_AreNotEqual()
    {
        // Arrange
        var config1 = BulkConfig.Default with { PropertiesToInclude = ["A", "B"] };
        var config2 = BulkConfig.Default with { PropertiesToInclude = ["C", "D"] };

        // Assert
        config1.ShouldNotBe(config2);
    }

    [Fact]
    public void Equality_SamePropertiesToInclude_AreEqual()
    {
        // Arrange
        var properties = new List<string> { "Status", "UpdatedAt" };
        var config1 = BulkConfig.Default with { PropertiesToInclude = properties };
        var config2 = BulkConfig.Default with { PropertiesToInclude = properties };

        // Assert
        config1.ShouldBe(config2);
    }

    [Fact]
    public void Equality_NullVsEmptyPropertiesToInclude_AreNotEqual()
    {
        // Arrange
        var config1 = BulkConfig.Default with { PropertiesToInclude = null };
        var config2 = BulkConfig.Default with { PropertiesToInclude = [] };

        // Assert
        config1.ShouldNotBe(config2);
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_SameValues_ReturnsSameHashCode()
    {
        // Arrange
        var config1 = BulkConfig.Default with { BatchSize = 1000, SetOutputIdentity = true };
        var config2 = BulkConfig.Default with { BatchSize = 1000, SetOutputIdentity = true };

        // Assert
        config1.GetHashCode().ShouldBe(config2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentValues_ReturnsDifferentHashCode()
    {
        // Arrange
        var config1 = BulkConfig.Default with { BatchSize = 1000 };
        var config2 = BulkConfig.Default with { BatchSize = 2000 };

        // Assert
        config1.GetHashCode().ShouldNotBe(config2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_IsConsistent()
    {
        // Arrange
        var config = BulkConfig.Default with { BatchSize = 5000 };

        // Act
        var hash1 = config.GetHashCode();
        var hash2 = config.GetHashCode();

        // Assert
        hash1.ShouldBe(hash2);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void BatchSize_CanBeZero()
    {
        // Act
        var config = BulkConfig.Default with { BatchSize = 0 };

        // Assert
        config.BatchSize.ShouldBe(0);
    }

    [Fact]
    public void BatchSize_CanBeNegative()
    {
        // Note: No validation in the record, validation happens at usage
        // Act
        var config = BulkConfig.Default with { BatchSize = -1 };

        // Assert
        config.BatchSize.ShouldBe(-1);
    }

    [Fact]
    public void BulkCopyTimeout_CanBeZero()
    {
        // Act (0 = no timeout in SqlBulkCopy)
        var config = BulkConfig.Default with { BulkCopyTimeout = 0 };

        // Assert
        config.BulkCopyTimeout.ShouldBe(0);
    }

    [Fact]
    public void PropertiesToInclude_EmptyList_IsValid()
    {
        // Act
        var config = BulkConfig.Default with { PropertiesToInclude = [] };

        // Assert
        config.PropertiesToInclude.ShouldNotBeNull();
        config.PropertiesToInclude.ShouldBeEmpty();
    }

    [Fact]
    public void PropertiesToExclude_EmptyList_IsValid()
    {
        // Act
        var config = BulkConfig.Default with { PropertiesToExclude = [] };

        // Assert
        config.PropertiesToExclude.ShouldNotBeNull();
        config.PropertiesToExclude.ShouldBeEmpty();
    }

    #endregion
}
