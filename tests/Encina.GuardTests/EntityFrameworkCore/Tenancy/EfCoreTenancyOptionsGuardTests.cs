using Encina.EntityFrameworkCore.Tenancy;
using Shouldly;

namespace Encina.GuardTests.EntityFrameworkCore.Tenancy;

/// <summary>
/// Guard clause tests for <see cref="EfCoreTenancyOptions"/>.
/// </summary>
/// <remarks>
/// EF Core tenancy options are simple POCO classes with boolean properties.
/// There are no null checks or validation in the constructor, so the guard tests
/// verify the default values are secure-by-default.
/// </remarks>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class EfCoreTenancyOptionsGuardTests
{
    [Fact]
    public void EfCoreTenancyOptions_DefaultValues_AreSecure()
    {
        // Arrange & Act
        var options = new EfCoreTenancyOptions();

        // Assert - All security options default to true
        options.AutoAssignTenantId.ShouldBeTrue("AutoAssignTenantId should default to true for convenience");
        options.ValidateTenantOnSave.ShouldBeTrue("ValidateTenantOnSave should default to true for security");
        options.UseQueryFilters.ShouldBeTrue("UseQueryFilters should default to true for isolation");
        options.ThrowOnMissingTenantContext.ShouldBeTrue("ThrowOnMissingTenantContext should default to true to prevent data leaks");
    }

    [Fact]
    public void EfCoreTenancyOptions_CanDisableAllOptions()
    {
        // Arrange
        var options = new EfCoreTenancyOptions
        {
            AutoAssignTenantId = false,
            ValidateTenantOnSave = false,
            UseQueryFilters = false,
            ThrowOnMissingTenantContext = false
        };

        // Assert
        options.AutoAssignTenantId.ShouldBeFalse();
        options.ValidateTenantOnSave.ShouldBeFalse();
        options.UseQueryFilters.ShouldBeFalse();
        options.ThrowOnMissingTenantContext.ShouldBeFalse();
    }

    [Fact]
    public void EfCoreTenancyOptions_CanToggleIndividualOptions()
    {
        // Arrange
        var options = new EfCoreTenancyOptions();

        // Act - Toggle each option
        options.AutoAssignTenantId = false;
        options.ValidateTenantOnSave = false;
        options.UseQueryFilters = false;
        options.ThrowOnMissingTenantContext = false;

        // Assert
        options.AutoAssignTenantId.ShouldBeFalse();
        options.ValidateTenantOnSave.ShouldBeFalse();
        options.UseQueryFilters.ShouldBeFalse();
        options.ThrowOnMissingTenantContext.ShouldBeFalse();

        // Toggle back
        options.AutoAssignTenantId = true;
        options.ValidateTenantOnSave = true;
        options.UseQueryFilters = true;
        options.ThrowOnMissingTenantContext = true;

        options.AutoAssignTenantId.ShouldBeTrue();
        options.ValidateTenantOnSave.ShouldBeTrue();
        options.UseQueryFilters.ShouldBeTrue();
        options.ThrowOnMissingTenantContext.ShouldBeTrue();
    }
}
