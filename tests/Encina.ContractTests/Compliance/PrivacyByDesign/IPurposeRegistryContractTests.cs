#pragma warning disable CA1859 // Contract tests intentionally use interface types

using Encina.Compliance.PrivacyByDesign;
using Encina.Compliance.PrivacyByDesign.Model;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using static LanguageExt.Prelude;

namespace Encina.ContractTests.Compliance.PrivacyByDesign;

#region Abstract Base Class

/// <summary>
/// Abstract contract tests for <see cref="IPurposeRegistry"/> verifying all implementations
/// behave consistently regardless of backing store technology.
/// </summary>
[Trait("Category", "Contract")]
public abstract class PurposeRegistryContractTestsBase
{
    protected abstract IPurposeRegistry CreateStore();

    #region RegisterThenGet Contract

    [Fact]
    public async Task Contract_RegisterThenGet_ReturnsSamePurpose()
    {
        IPurposeRegistry registry = CreateStore();
        var purpose = CreatePurpose("Order Processing");

        var registerResult = await registry.RegisterPurposeAsync(purpose);
        registerResult.IsRight.ShouldBeTrue("Register should succeed");

        var getResult = await registry.GetPurposeAsync("Order Processing");
        getResult.IsRight.ShouldBeTrue("Get should succeed");

        var option = getResult.Match(o => o, _ => Option<PurposeDefinition>.None);
        option.IsSome.ShouldBeTrue("Should find the registered purpose");

        var retrieved = (PurposeDefinition)option;
        retrieved.PurposeId.ShouldBe(purpose.PurposeId);
        retrieved.Name.ShouldBe("Order Processing");
        retrieved.LegalBasis.ShouldBe("Contract");
        retrieved.AllowedFields.Count.ShouldBe(2);
    }

    #endregion

    #region RegisterDuplicate Contract

    [Fact]
    public async Task Contract_RegisterDuplicate_ReturnsError()
    {
        IPurposeRegistry registry = CreateStore();
        var purpose1 = CreatePurpose("Analytics");
        var purpose2 = CreatePurpose("Analytics"); // Same name, different PurposeId

        var result1 = await registry.RegisterPurposeAsync(purpose1);
        result1.IsRight.ShouldBeTrue("First registration should succeed");

        var result2 = await registry.RegisterPurposeAsync(purpose2);
        result2.IsLeft.ShouldBeTrue("Duplicate registration should return error");
    }

    #endregion

    #region GetPurpose NonExistent Contract

    [Fact]
    public async Task Contract_GetPurpose_NonExistent_ReturnsNone()
    {
        IPurposeRegistry registry = CreateStore();

        var result = await registry.GetPurposeAsync("NonExistent Purpose");
        result.IsRight.ShouldBeTrue("Get should succeed even when not found");

        var option = result.Match(o => o, _ => Option<PurposeDefinition>.None);
        option.IsNone.ShouldBeTrue("Should return None for non-existent purpose");
    }

    #endregion

    #region ModuleFallback Contract

    [Fact]
    public async Task Contract_GetPurpose_ModuleFallback_ReturnsGlobal()
    {
        IPurposeRegistry registry = CreateStore();
        var globalPurpose = CreatePurpose("Shared Purpose");

        await registry.RegisterPurposeAsync(globalPurpose);

        // Query with a moduleId, but only a global purpose exists — should fall back.
        var result = await registry.GetPurposeAsync("Shared Purpose", moduleId: "sales-module");
        result.IsRight.ShouldBeTrue("Get should succeed");

        var option = result.Match(o => o, _ => Option<PurposeDefinition>.None);
        option.IsSome.ShouldBeTrue("Should fall back to global purpose");

        var retrieved = (PurposeDefinition)option;
        retrieved.PurposeId.ShouldBe(globalPurpose.PurposeId);
        retrieved.ModuleId.ShouldBeNull();
    }

    #endregion

    #region ModuleSpecific TakesPrecedence Contract

    [Fact]
    public async Task Contract_GetPurpose_ModuleSpecific_TakesPrecedence()
    {
        IPurposeRegistry registry = CreateStore();
        var globalPurpose = CreatePurpose("Data Processing");
        var modulePurpose = CreatePurpose("Data Processing", moduleId: "billing-module");

        await registry.RegisterPurposeAsync(globalPurpose);
        await registry.RegisterPurposeAsync(modulePurpose);

        // Query with moduleId — module-specific should take precedence.
        var result = await registry.GetPurposeAsync("Data Processing", moduleId: "billing-module");
        result.IsRight.ShouldBeTrue("Get should succeed");

        var option = result.Match(o => o, _ => Option<PurposeDefinition>.None);
        option.IsSome.ShouldBeTrue("Should find module-specific purpose");

        var retrieved = (PurposeDefinition)option;
        retrieved.PurposeId.ShouldBe(modulePurpose.PurposeId);
        retrieved.ModuleId.ShouldBe("billing-module");
    }

    #endregion

    #region GetAllPurposes Contract

    [Fact]
    public async Task Contract_GetAllPurposes_ReturnsAllRegistered()
    {
        IPurposeRegistry registry = CreateStore();
        var purpose1 = CreatePurpose("Purpose A");
        var purpose2 = CreatePurpose("Purpose B");
        var purpose3 = CreatePurpose("Purpose C");

        await registry.RegisterPurposeAsync(purpose1);
        await registry.RegisterPurposeAsync(purpose2);
        await registry.RegisterPurposeAsync(purpose3);

        var result = await registry.GetAllPurposesAsync();
        result.IsRight.ShouldBeTrue("GetAll should succeed");

        var purposes = result.Match(p => p, _ => []);
        purposes.Count.ShouldBe(3);
    }

    #endregion

    #region GetAllPurposes WithModuleId Contract

    [Fact]
    public async Task Contract_GetAllPurposes_WithModuleId_MergesModuleAndGlobal()
    {
        IPurposeRegistry registry = CreateStore();
        var globalPurpose = CreatePurpose("Global Purpose");
        var modulePurpose = CreatePurpose("Module Purpose", moduleId: "crm-module");

        await registry.RegisterPurposeAsync(globalPurpose);
        await registry.RegisterPurposeAsync(modulePurpose);

        var result = await registry.GetAllPurposesAsync(moduleId: "crm-module");
        result.IsRight.ShouldBeTrue("GetAll with moduleId should succeed");

        var purposes = result.Match(p => p, _ => []);
        purposes.Count.ShouldBe(2, "Should include both global and module-specific purposes");

        purposes.ShouldContain(p => p.Name == "Global Purpose");
        purposes.ShouldContain(p => p.Name == "Module Purpose");
    }

    #endregion

    #region RemovePurpose Contract

    [Fact]
    public async Task Contract_RemovePurpose_Succeeds()
    {
        IPurposeRegistry registry = CreateStore();
        var purpose = CreatePurpose("Temporary Purpose");

        await registry.RegisterPurposeAsync(purpose);

        var removeResult = await registry.RemovePurposeAsync(purpose.PurposeId);
        removeResult.IsRight.ShouldBeTrue("Remove should succeed");

        var getResult = await registry.GetPurposeAsync("Temporary Purpose");
        getResult.IsRight.ShouldBeTrue("Get after remove should succeed");

        var option = getResult.Match(o => o, _ => Option<PurposeDefinition>.None);
        option.IsNone.ShouldBeTrue("Purpose should no longer exist after removal");
    }

    #endregion

    #region RemovePurpose NonExistent Contract

    [Fact]
    public async Task Contract_RemovePurpose_NonExistent_ReturnsError()
    {
        IPurposeRegistry registry = CreateStore();

        var result = await registry.RemovePurposeAsync("non-existent-id");
        result.IsLeft.ShouldBeTrue("Removing a non-existent purpose should return error");
    }

    #endregion

    #region Helpers

    protected static PurposeDefinition CreatePurpose(string name, string? moduleId = null) => new()
    {
        PurposeId = Guid.NewGuid().ToString(),
        Name = name,
        Description = "Test purpose",
        LegalBasis = "Contract",
        AllowedFields = ["Field1", "Field2"],
        ModuleId = moduleId,
        CreatedAtUtc = DateTimeOffset.UtcNow
    };

    #endregion
}

#endregion

#region InMemory Implementation Tests

/// <summary>
/// Contract tests for <see cref="InMemoryPurposeRegistry"/>.
/// </summary>
[Trait("Category", "Contract")]
public sealed class InMemoryPurposeRegistryContractTests : PurposeRegistryContractTestsBase
{
    protected override IPurposeRegistry CreateStore() =>
        new InMemoryPurposeRegistry(NullLogger<InMemoryPurposeRegistry>.Instance);
}

#endregion
