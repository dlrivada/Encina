#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.PrivacyByDesign;
using Encina.Compliance.PrivacyByDesign.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Compliance.PrivacyByDesign;

/// <summary>
/// Unit tests for <see cref="InMemoryPurposeRegistry"/>.
/// </summary>
public class InMemoryPurposeRegistryTests
{
    private readonly InMemoryPurposeRegistry _sut;

    public InMemoryPurposeRegistryTests()
    {
        _sut = new InMemoryPurposeRegistry(NullLogger<InMemoryPurposeRegistry>.Instance);
    }

    private static PurposeDefinition CreatePurpose(
        string name = "Order Processing",
        string? moduleId = null,
        string? purposeId = null) => new()
    {
        PurposeId = purposeId ?? Guid.NewGuid().ToString(),
        Name = name,
        Description = "Test purpose",
        LegalBasis = "Contract",
        AllowedFields = ["ProductId", "Quantity"],
        ModuleId = moduleId,
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    #region RegisterPurposeAsync

    [Fact]
    public async Task RegisterPurposeAsync_ValidPurpose_ShouldReturnRightUnit()
    {
        // Arrange
        var purpose = CreatePurpose();

        // Act
        var result = await _sut.RegisterPurposeAsync(purpose);

        // Assert
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterPurposeAsync_ValidPurpose_ShouldIncrementCount()
    {
        // Arrange
        var purpose = CreatePurpose();
        _sut.Count.Should().Be(0);

        // Act
        await _sut.RegisterPurposeAsync(purpose);

        // Assert
        _sut.Count.Should().Be(1);
    }

    [Fact]
    public async Task RegisterPurposeAsync_DuplicateNameAndModuleIdWithDifferentPurposeId_ShouldReturnLeftError()
    {
        // Arrange
        var purpose1 = CreatePurpose("Order Processing", purposeId: "id-1");
        var purpose2 = CreatePurpose("Order Processing", purposeId: "id-2");
        await _sut.RegisterPurposeAsync(purpose1);

        // Act
        var result = await _sut.RegisterPurposeAsync(purpose2);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetEncinaCode().Should().Be(PrivacyByDesignErrors.DuplicatePurposeCode);
    }

    [Fact]
    public async Task RegisterPurposeAsync_SamePurposeId_ShouldUpsertExisting()
    {
        // Arrange
        var purposeId = Guid.NewGuid().ToString();
        var original = CreatePurpose("Original Name", purposeId: purposeId);
        var updated = new PurposeDefinition
        {
            PurposeId = purposeId,
            Name = "Updated Name",
            Description = "Updated description",
            LegalBasis = "Consent",
            AllowedFields = ["Email"],
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        await _sut.RegisterPurposeAsync(original);

        // Act
        var result = await _sut.RegisterPurposeAsync(updated);

        // Assert
        result.IsRight.Should().BeTrue();
        _sut.Count.Should().Be(1);

        var getResult = await _sut.GetPurposeAsync("Updated Name");
        getResult.IsRight.Should().BeTrue();
        var option = (Option<PurposeDefinition>)getResult;
        option.IsSome.Should().BeTrue();
        option.IfSome(p =>
        {
            p.Description.Should().Be("Updated description");
            p.LegalBasis.Should().Be("Consent");
        });
    }

    #endregion

    #region GetPurposeAsync

    [Fact]
    public async Task GetPurposeAsync_ExistingPurpose_ShouldReturnSome()
    {
        // Arrange
        var purpose = CreatePurpose();
        await _sut.RegisterPurposeAsync(purpose);

        // Act
        var result = await _sut.GetPurposeAsync(purpose.Name);

        // Assert
        result.IsRight.Should().BeTrue();
        var option = (Option<PurposeDefinition>)result;
        option.IsSome.Should().BeTrue();
        option.IfSome(p => p.PurposeId.Should().Be(purpose.PurposeId));
    }

    [Fact]
    public async Task GetPurposeAsync_NonExistentPurpose_ShouldReturnNone()
    {
        // Act
        var result = await _sut.GetPurposeAsync("NonExistent");

        // Assert
        result.IsRight.Should().BeTrue();
        var option = (Option<PurposeDefinition>)result;
        option.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task GetPurposeAsync_ModuleSpecificLookup_ShouldReturnModulePurpose()
    {
        // Arrange
        var modulePurpose = CreatePurpose("Analytics", moduleId: "sales");
        await _sut.RegisterPurposeAsync(modulePurpose);

        // Act
        var result = await _sut.GetPurposeAsync("Analytics", moduleId: "sales");

        // Assert
        result.IsRight.Should().BeTrue();
        var option = (Option<PurposeDefinition>)result;
        option.IsSome.Should().BeTrue();
        option.IfSome(p => p.ModuleId.Should().Be("sales"));
    }

    [Fact]
    public async Task GetPurposeAsync_ModuleFallback_ShouldReturnGlobalWhenModuleSpecificDoesNotExist()
    {
        // Arrange
        var globalPurpose = CreatePurpose("Shared Purpose", moduleId: null);
        await _sut.RegisterPurposeAsync(globalPurpose);

        // Act
        var result = await _sut.GetPurposeAsync("Shared Purpose", moduleId: "any-module");

        // Assert
        result.IsRight.Should().BeTrue();
        var option = (Option<PurposeDefinition>)result;
        option.IsSome.Should().BeTrue();
        option.IfSome(p => p.ModuleId.Should().BeNull());
    }

    [Fact]
    public async Task GetPurposeAsync_NullPurposeName_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _sut.GetPurposeAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region GetAllPurposesAsync

    [Fact]
    public async Task GetAllPurposesAsync_NullModuleId_ShouldReturnOnlyGlobalPurposes()
    {
        // Arrange
        var global1 = CreatePurpose("Global 1");
        var global2 = CreatePurpose("Global 2");
        var modulePurpose = CreatePurpose("Module Only", moduleId: "sales");
        await _sut.RegisterPurposeAsync(global1);
        await _sut.RegisterPurposeAsync(global2);
        await _sut.RegisterPurposeAsync(modulePurpose);

        // Act
        var result = await _sut.GetAllPurposesAsync(moduleId: null);

        // Assert
        result.IsRight.Should().BeTrue();
        var purposes = result.Match(r => r, _ => (IReadOnlyList<PurposeDefinition>)[]);
        purposes.Should().HaveCount(2);
        purposes.Should().Contain(p => p.Name == "Global 1");
        purposes.Should().Contain(p => p.Name == "Global 2");
    }

    [Fact]
    public async Task GetAllPurposesAsync_WithModuleId_ShouldReturnModuleAndGlobalPurposes()
    {
        // Arrange
        var globalPurpose = CreatePurpose("Global Purpose");
        var modulePurpose = CreatePurpose("Module Purpose", moduleId: "sales");
        var otherModulePurpose = CreatePurpose("Other Module Purpose", moduleId: "marketing");
        await _sut.RegisterPurposeAsync(globalPurpose);
        await _sut.RegisterPurposeAsync(modulePurpose);
        await _sut.RegisterPurposeAsync(otherModulePurpose);

        // Act
        var result = await _sut.GetAllPurposesAsync(moduleId: "sales");

        // Assert
        result.IsRight.Should().BeTrue();
        var purposes = result.Match(r => r, _ => (IReadOnlyList<PurposeDefinition>)[]);
        purposes.Should().HaveCount(2);
        purposes.Should().Contain(p => p.Name == "Global Purpose");
        purposes.Should().Contain(p => p.Name == "Module Purpose");
        purposes.Should().NotContain(p => p.Name == "Other Module Purpose");
    }

    [Fact]
    public async Task GetAllPurposesAsync_ModuleOverridesGlobalWithSameName()
    {
        // Arrange
        var globalPurpose = CreatePurpose("Shared Purpose", moduleId: null);
        var modulePurpose = CreatePurpose("Shared Purpose", moduleId: "sales");
        await _sut.RegisterPurposeAsync(globalPurpose);
        await _sut.RegisterPurposeAsync(modulePurpose);

        // Act
        var result = await _sut.GetAllPurposesAsync(moduleId: "sales");

        // Assert
        result.IsRight.Should().BeTrue();
        var purposes = result.Match(r => r, _ => (IReadOnlyList<PurposeDefinition>)[]);

        // Module overrides global for same name, so only 1 result
        purposes.Should().HaveCount(1);
        purposes[0].ModuleId.Should().Be("sales");
    }

    [Fact]
    public async Task GetAllPurposesAsync_EmptyRegistry_ShouldReturnEmptyList()
    {
        // Act
        var result = await _sut.GetAllPurposesAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var purposes = result.Match(r => r, _ => (IReadOnlyList<PurposeDefinition>)[]);
        purposes.Should().BeEmpty();
    }

    #endregion

    #region RemovePurposeAsync

    [Fact]
    public async Task RemovePurposeAsync_ExistingPurpose_ShouldRemoveSuccessfully()
    {
        // Arrange
        var purpose = CreatePurpose();
        await _sut.RegisterPurposeAsync(purpose);

        // Act
        var result = await _sut.RemovePurposeAsync(purpose.PurposeId);

        // Assert
        result.IsRight.Should().BeTrue();
        _sut.Count.Should().Be(0);
    }

    [Fact]
    public async Task RemovePurposeAsync_NonExistentPurposeId_ShouldReturnError()
    {
        // Act
        var result = await _sut.RemovePurposeAsync("non-existent-id");

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetEncinaCode().Should().Be(PrivacyByDesignErrors.PurposeNotFoundCode);
    }

    [Fact]
    public async Task RemovePurposeAsync_AfterRemoval_GetPurposeShouldReturnNone()
    {
        // Arrange
        var purpose = CreatePurpose();
        await _sut.RegisterPurposeAsync(purpose);

        // Act
        await _sut.RemovePurposeAsync(purpose.PurposeId);

        // Assert
        var getResult = await _sut.GetPurposeAsync(purpose.Name);
        getResult.IsRight.Should().BeTrue();
        var option = (Option<PurposeDefinition>)getResult;
        option.IsNone.Should().BeTrue();
    }

    #endregion
}
