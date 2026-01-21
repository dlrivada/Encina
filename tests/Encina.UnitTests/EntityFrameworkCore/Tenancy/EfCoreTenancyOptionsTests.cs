using Encina.EntityFrameworkCore.Tenancy;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Tenancy;

/// <summary>
/// Unit tests for <see cref="EfCoreTenancyOptions"/>.
/// </summary>
public sealed class EfCoreTenancyOptionsTests
{
    [Fact]
    public void Constructor_DefaultValues_AreCorrect()
    {
        // Act
        var options = new EfCoreTenancyOptions();

        // Assert
        options.AutoAssignTenantId.ShouldBeTrue();
        options.ValidateTenantOnSave.ShouldBeTrue();
        options.UseQueryFilters.ShouldBeTrue();
        options.ThrowOnMissingTenantContext.ShouldBeTrue();
    }

    [Fact]
    public void AutoAssignTenantId_CanBeSet()
    {
        // Arrange
        var options = new EfCoreTenancyOptions();

        // Act
        options.AutoAssignTenantId = false;

        // Assert
        options.AutoAssignTenantId.ShouldBeFalse();
    }

    [Fact]
    public void ValidateTenantOnSave_CanBeSet()
    {
        // Arrange
        var options = new EfCoreTenancyOptions();

        // Act
        options.ValidateTenantOnSave = false;

        // Assert
        options.ValidateTenantOnSave.ShouldBeFalse();
    }

    [Fact]
    public void UseQueryFilters_CanBeSet()
    {
        // Arrange
        var options = new EfCoreTenancyOptions();

        // Act
        options.UseQueryFilters = false;

        // Assert
        options.UseQueryFilters.ShouldBeFalse();
    }

    [Fact]
    public void ThrowOnMissingTenantContext_CanBeSet()
    {
        // Arrange
        var options = new EfCoreTenancyOptions();

        // Act
        options.ThrowOnMissingTenantContext = false;

        // Assert
        options.ThrowOnMissingTenantContext.ShouldBeFalse();
    }
}
