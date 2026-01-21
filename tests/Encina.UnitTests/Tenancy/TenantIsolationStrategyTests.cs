using Encina.Tenancy;

namespace Encina.UnitTests.Tenancy;

/// <summary>
/// Unit tests for <see cref="TenantIsolationStrategy"/>.
/// </summary>
public class TenantIsolationStrategyTests
{
    [Fact]
    public void TenantIsolationStrategy_HasExpectedValues()
    {
        // Assert
        ((int)TenantIsolationStrategy.SharedSchema).ShouldBe(0);
        ((int)TenantIsolationStrategy.SchemaPerTenant).ShouldBe(1);
        ((int)TenantIsolationStrategy.DatabasePerTenant).ShouldBe(2);
    }

    [Fact]
    public void TenantIsolationStrategy_SharedSchema_IsDefault()
    {
        // Arrange
        TenantIsolationStrategy defaultValue = default;

        // Assert
        defaultValue.ShouldBe(TenantIsolationStrategy.SharedSchema);
    }

    [Theory]
    [InlineData(TenantIsolationStrategy.SharedSchema)]
    [InlineData(TenantIsolationStrategy.SchemaPerTenant)]
    [InlineData(TenantIsolationStrategy.DatabasePerTenant)]
    public void TenantIsolationStrategy_AllValues_CanBeAssigned(TenantIsolationStrategy strategy)
    {
        // Arrange & Act
        var options = new TenancyOptions { DefaultStrategy = strategy };

        // Assert
        options.DefaultStrategy.ShouldBe(strategy);
    }
}
