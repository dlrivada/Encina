#pragma warning disable CA1859 // Contract tests intentionally use interface types

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;

using LanguageExt;

namespace Encina.ContractTests.Compliance.ProcessorAgreements;

#region Abstract Base Class

/// <summary>
/// Abstract contract tests for <see cref="IProcessorRegistry"/> verifying all implementations
/// behave consistently regardless of backing store technology.
/// </summary>
[Trait("Category", "Contract")]
public abstract class ProcessorRegistryContractTestsBase
{
    protected abstract IProcessorRegistry CreateStore();

    #region RegisterProcessorAsync Contract

    [Fact]
    public async Task Contract_RegisterThenGet_ReturnsSameProcessor()
    {
        var store = CreateStore();
        var processor = CreateProcessor("p1", "Test Processor");

        var registerResult = await store.RegisterProcessorAsync(processor);
        registerResult.IsRight.ShouldBeTrue("Register should succeed");

        var getResult = await store.GetProcessorAsync("p1");
        getResult.IsRight.ShouldBeTrue("Get should succeed");

        var option = getResult.Match(o => o, _ => Option<Processor>.None);
        option.IsSome.ShouldBeTrue("Processor should be found");
        var retrieved = (Processor)option;
        retrieved.Id.ShouldBe("p1");
        retrieved.Name.ShouldBe("Test Processor");
        retrieved.Country.ShouldBe("DE");
    }

    [Fact]
    public async Task Contract_RegisterDuplicate_ReturnsError()
    {
        var store = CreateStore();
        var processor = CreateProcessor("dup1", "Original");

        var first = await store.RegisterProcessorAsync(processor);
        first.IsRight.ShouldBeTrue("First register should succeed");

        var second = await store.RegisterProcessorAsync(processor);
        second.IsLeft.ShouldBeTrue("Duplicate register should return error");
    }

    #endregion

    #region GetProcessorAsync Contract

    [Fact]
    public async Task Contract_GetProcessor_NonExistent_ReturnsNone()
    {
        var store = CreateStore();

        var result = await store.GetProcessorAsync("non-existent");
        result.IsRight.ShouldBeTrue("Get non-existent should succeed");

        var option = result.Match(o => o, _ => Option<Processor>.None);
        option.IsNone.ShouldBeTrue("Non-existent processor should return None");
    }

    #endregion

    #region GetAllProcessorsAsync Contract

    [Fact]
    public async Task Contract_GetAllProcessors_ReturnsAllRegistered()
    {
        var store = CreateStore();

        await store.RegisterProcessorAsync(CreateProcessor("all1", "First"));
        await store.RegisterProcessorAsync(CreateProcessor("all2", "Second"));
        await store.RegisterProcessorAsync(CreateProcessor("all3", "Third"));

        var result = await store.GetAllProcessorsAsync();
        result.IsRight.ShouldBeTrue("GetAll should succeed");

        var list = result.Match(l => l, _ => (IReadOnlyList<Processor>)[]);
        list.Count.ShouldBeGreaterThanOrEqualTo(3);
    }

    #endregion

    #region UpdateProcessorAsync Contract

    [Fact]
    public async Task Contract_UpdateProcessor_ModifiesStored()
    {
        var store = CreateStore();
        var processor = CreateProcessor("upd1", "Original Name");
        await store.RegisterProcessorAsync(processor);

        var updated = processor with { Name = "Updated Name", LastUpdatedAtUtc = DateTimeOffset.UtcNow };
        var updateResult = await store.UpdateProcessorAsync(updated);
        updateResult.IsRight.ShouldBeTrue("Update should succeed");

        var getResult = await store.GetProcessorAsync("upd1");
        var option = getResult.Match(o => o, _ => Option<Processor>.None);
        option.IsSome.ShouldBeTrue();
        var retrieved = (Processor)option;
        retrieved.Name.ShouldBe("Updated Name");
    }

    [Fact]
    public async Task Contract_UpdateProcessor_NonExistent_ReturnsError()
    {
        var store = CreateStore();
        var processor = CreateProcessor("no-exist", "Ghost");

        var result = await store.UpdateProcessorAsync(processor);
        result.IsLeft.ShouldBeTrue("Update non-existent should return error");
    }

    #endregion

    #region RemoveProcessorAsync Contract

    [Fact]
    public async Task Contract_RemoveProcessor_Succeeds()
    {
        var store = CreateStore();
        var processor = CreateProcessor("rem1", "ToRemove");
        await store.RegisterProcessorAsync(processor);

        var removeResult = await store.RemoveProcessorAsync("rem1");
        removeResult.IsRight.ShouldBeTrue("Remove should succeed");

        var getResult = await store.GetProcessorAsync("rem1");
        var option = getResult.Match(o => o, _ => Option<Processor>.None);
        option.IsNone.ShouldBeTrue("Removed processor should no longer be retrievable");
    }

    [Fact]
    public async Task Contract_RemoveProcessor_NonExistent_ReturnsError()
    {
        var store = CreateStore();

        var result = await store.RemoveProcessorAsync("no-exist");
        result.IsLeft.ShouldBeTrue("Remove non-existent should return error");
    }

    #endregion

    #region GetSubProcessorsAsync Contract

    [Fact]
    public async Task Contract_GetSubProcessors_ReturnsDirectChildren()
    {
        var store = CreateStore();

        var parent = CreateProcessor("parent1", "Parent");
        await store.RegisterProcessorAsync(parent);

        var child1 = CreateSubProcessor("child1", "Child 1", "parent1", 1);
        var child2 = CreateSubProcessor("child2", "Child 2", "parent1", 1);
        await store.RegisterProcessorAsync(child1);
        await store.RegisterProcessorAsync(child2);

        var result = await store.GetSubProcessorsAsync("parent1");
        result.IsRight.ShouldBeTrue("GetSubProcessors should succeed");

        var list = result.Match(l => l, _ => (IReadOnlyList<Processor>)[]);
        list.Count.ShouldBe(2);
        list.ShouldContain(p => p.Id == "child1");
        list.ShouldContain(p => p.Id == "child2");
    }

    [Fact]
    public async Task Contract_GetSubProcessors_NoChildren_ReturnsEmptyList()
    {
        var store = CreateStore();
        var processor = CreateProcessor("lonely1", "No Children");
        await store.RegisterProcessorAsync(processor);

        var result = await store.GetSubProcessorsAsync("lonely1");
        result.IsRight.ShouldBeTrue();

        var list = result.Match(l => l, _ => (IReadOnlyList<Processor>)[]);
        list.Count.ShouldBe(0);
    }

    #endregion

    #region GetFullSubProcessorChainAsync Contract

    [Fact]
    public async Task Contract_GetFullSubProcessorChain_ReturnsAllDescendants()
    {
        var store = CreateStore();

        var root = CreateProcessor("root1", "Root");
        await store.RegisterProcessorAsync(root);

        var level1 = CreateSubProcessor("l1-1", "Level 1", "root1", 1);
        await store.RegisterProcessorAsync(level1);

        var level2 = CreateSubProcessor("l2-1", "Level 2", "l1-1", 2);
        await store.RegisterProcessorAsync(level2);

        var result = await store.GetFullSubProcessorChainAsync("root1");
        result.IsRight.ShouldBeTrue("GetFullChain should succeed");

        var list = result.Match(l => l, _ => (IReadOnlyList<Processor>)[]);
        list.Count.ShouldBe(2);
        list.ShouldContain(p => p.Id == "l1-1");
        list.ShouldContain(p => p.Id == "l2-1");
    }

    [Fact]
    public async Task Contract_GetFullSubProcessorChain_NoDescendants_ReturnsEmptyList()
    {
        var store = CreateStore();
        var processor = CreateProcessor("leaf1", "Leaf");
        await store.RegisterProcessorAsync(processor);

        var result = await store.GetFullSubProcessorChainAsync("leaf1");
        result.IsRight.ShouldBeTrue();

        var list = result.Match(l => l, _ => (IReadOnlyList<Processor>)[]);
        list.Count.ShouldBe(0);
    }

    #endregion

    #region Helpers

    protected static Processor CreateProcessor(string id, string name) => new()
    {
        Id = id,
        Name = name,
        Country = "DE",
        Depth = 0,
        SubProcessorAuthorizationType = SubProcessorAuthorizationType.General,
        CreatedAtUtc = DateTimeOffset.UtcNow,
        LastUpdatedAtUtc = DateTimeOffset.UtcNow
    };

    protected static Processor CreateSubProcessor(
        string id, string name, string parentId, int depth) => new()
    {
        Id = id,
        Name = name,
        Country = "DE",
        ParentProcessorId = parentId,
        Depth = depth,
        SubProcessorAuthorizationType = SubProcessorAuthorizationType.Specific,
        CreatedAtUtc = DateTimeOffset.UtcNow,
        LastUpdatedAtUtc = DateTimeOffset.UtcNow
    };

    #endregion
}

#endregion
