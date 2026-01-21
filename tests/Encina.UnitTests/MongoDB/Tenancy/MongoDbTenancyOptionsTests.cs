using Encina.MongoDB.Tenancy;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.MongoDB.Tenancy;

/// <summary>
/// Unit tests for <see cref="MongoDbTenancyOptions"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class MongoDbTenancyOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new MongoDbTenancyOptions();

        // Assert
        options.AutoFilterTenantQueries.ShouldBeTrue();
        options.AutoAssignTenantId.ShouldBeTrue();
        options.ValidateTenantOnModify.ShouldBeTrue();
        options.ThrowOnMissingTenantContext.ShouldBeTrue();
        options.TenantFieldName.ShouldBe("TenantId");
        options.EnableDatabasePerTenant.ShouldBeFalse();
        options.DatabaseNamePattern.ShouldBe("{baseName}_{tenantId}");
    }

    [Fact]
    public void AutoFilterTenantQueries_ShouldBeConfigurable()
    {
        // Arrange
        var options = new MongoDbTenancyOptions();

        // Act
        options.AutoFilterTenantQueries = false;

        // Assert
        options.AutoFilterTenantQueries.ShouldBeFalse();
    }

    [Fact]
    public void AutoAssignTenantId_ShouldBeConfigurable()
    {
        // Arrange
        var options = new MongoDbTenancyOptions();

        // Act
        options.AutoAssignTenantId = false;

        // Assert
        options.AutoAssignTenantId.ShouldBeFalse();
    }

    [Fact]
    public void ValidateTenantOnModify_ShouldBeConfigurable()
    {
        // Arrange
        var options = new MongoDbTenancyOptions();

        // Act
        options.ValidateTenantOnModify = false;

        // Assert
        options.ValidateTenantOnModify.ShouldBeFalse();
    }

    [Fact]
    public void ThrowOnMissingTenantContext_ShouldBeConfigurable()
    {
        // Arrange
        var options = new MongoDbTenancyOptions();

        // Act
        options.ThrowOnMissingTenantContext = false;

        // Assert
        options.ThrowOnMissingTenantContext.ShouldBeFalse();
    }

    [Fact]
    public void TenantFieldName_ShouldBeConfigurable()
    {
        // Arrange
        var options = new MongoDbTenancyOptions();

        // Act
        options.TenantFieldName = "OrganizationId";

        // Assert
        options.TenantFieldName.ShouldBe("OrganizationId");
    }

    [Fact]
    public void EnableDatabasePerTenant_ShouldBeConfigurable()
    {
        // Arrange
        var options = new MongoDbTenancyOptions();

        // Act
        options.EnableDatabasePerTenant = true;

        // Assert
        options.EnableDatabasePerTenant.ShouldBeTrue();
    }

    [Fact]
    public void DatabaseNamePattern_ShouldBeConfigurable()
    {
        // Arrange
        var options = new MongoDbTenancyOptions();

        // Act
        options.DatabaseNamePattern = "tenant_{tenantId}";

        // Assert
        options.DatabaseNamePattern.ShouldBe("tenant_{tenantId}");
    }

    [Fact]
    public void GetDatabaseName_WithDefaultPattern_ReturnsExpectedName()
    {
        // Arrange
        var options = new MongoDbTenancyOptions();

        // Act
        var result = options.GetDatabaseName("MyApp", "tenant-123");

        // Assert
        result.ShouldBe("MyApp_tenant-123");
    }

    [Fact]
    public void GetDatabaseName_WithCustomPattern_ReturnsExpectedName()
    {
        // Arrange
        var options = new MongoDbTenancyOptions
        {
            DatabaseNamePattern = "db_{tenantId}_{baseName}"
        };

        // Act
        var result = options.GetDatabaseName("MyApp", "tenant-123");

        // Assert
        result.ShouldBe("db_tenant-123_MyApp");
    }

    [Fact]
    public void GetDatabaseName_WithTenantIdOnlyPattern_ReturnsExpectedName()
    {
        // Arrange
        var options = new MongoDbTenancyOptions
        {
            DatabaseNamePattern = "tenant_{tenantId}"
        };

        // Act
        var result = options.GetDatabaseName("MyApp", "abc");

        // Assert
        result.ShouldBe("tenant_abc");
    }

    [Fact]
    public void GetDatabaseName_NullBaseName_ThrowsArgumentException()
    {
        // Arrange
        var options = new MongoDbTenancyOptions();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            options.GetDatabaseName(null!, "tenant-123"));
    }

    [Fact]
    public void GetDatabaseName_EmptyBaseName_ThrowsArgumentException()
    {
        // Arrange
        var options = new MongoDbTenancyOptions();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            options.GetDatabaseName(string.Empty, "tenant-123"));
    }

    [Fact]
    public void GetDatabaseName_NullTenantId_ThrowsArgumentException()
    {
        // Arrange
        var options = new MongoDbTenancyOptions();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            options.GetDatabaseName("MyApp", null!));
    }

    [Fact]
    public void GetDatabaseName_EmptyTenantId_ThrowsArgumentException()
    {
        // Arrange
        var options = new MongoDbTenancyOptions();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            options.GetDatabaseName("MyApp", string.Empty));
    }
}
