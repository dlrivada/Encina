#pragma warning disable CA2012

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="InMemoryProcessorRegistry"/>.
/// </summary>
public class InMemoryProcessorRegistryTests
{
    private readonly InMemoryProcessorRegistry _registry;

    public InMemoryProcessorRegistryTests()
    {
        _registry = new InMemoryProcessorRegistry(NullLogger<InMemoryProcessorRegistry>.Instance);
    }

    #region Helpers

    private static Processor CreateProcessor(
        string id = "proc-1",
        string name = "Test Processor",
        string country = "DE",
        string? parentProcessorId = null,
        int depth = 0,
        SubProcessorAuthorizationType authType = SubProcessorAuthorizationType.General)
    {
        var now = DateTimeOffset.UtcNow;
        return new Processor
        {
            Id = id,
            Name = name,
            Country = country,
            ParentProcessorId = parentProcessorId,
            Depth = depth,
            SubProcessorAuthorizationType = authType,
            CreatedAtUtc = now,
            LastUpdatedAtUtc = now
        };
    }

    #endregion

    #region RegisterProcessorAsync

    [Fact]
    public async Task RegisterProcessorAsync_ValidProcessor_ShouldSucceed()
    {
        var processor = CreateProcessor();

        var result = await _registry.RegisterProcessorAsync(processor);

        result.IsRight.Should().BeTrue();
        _registry.Count.Should().Be(1);
    }

    [Fact]
    public async Task RegisterProcessorAsync_Duplicate_ShouldReturnAlreadyExistsError()
    {
        var processor = CreateProcessor();
        await _registry.RegisterProcessorAsync(processor);

        var result = await _registry.RegisterProcessorAsync(processor);

        result.IsLeft.Should().BeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).Should().Be(ProcessorAgreementErrors.AlreadyExistsCode);
    }

    [Fact]
    public async Task RegisterProcessorAsync_DepthExceedsMax_ShouldReturnDepthExceededError()
    {
        _registry.MaxSubProcessorDepth = 2;
        var processor = CreateProcessor(depth: 3);

        var result = await _registry.RegisterProcessorAsync(processor);

        result.IsLeft.Should().BeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).Should().Be(ProcessorAgreementErrors.SubProcessorDepthExceededCode);
    }

    [Fact]
    public async Task RegisterProcessorAsync_SubProcessorWithNonExistentParent_ShouldReturnNotFoundError()
    {
        var sub = CreateProcessor(id: "sub-1", parentProcessorId: "non-existent", depth: 1);

        var result = await _registry.RegisterProcessorAsync(sub);

        result.IsLeft.Should().BeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).Should().Be(ProcessorAgreementErrors.NotFoundCode);
    }

    [Fact]
    public async Task RegisterProcessorAsync_SubProcessorWithInconsistentDepth_ShouldReturnValidationFailedError()
    {
        var parent = CreateProcessor(id: "parent", depth: 0);
        await _registry.RegisterProcessorAsync(parent);

        // Depth should be 1 (parent.Depth + 1), but we set it to 3.
        var sub = CreateProcessor(id: "sub-1", parentProcessorId: "parent", depth: 3);

        var result = await _registry.RegisterProcessorAsync(sub);

        result.IsLeft.Should().BeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).Should().Be(ProcessorAgreementErrors.ValidationFailedCode);
    }

    [Fact]
    public async Task RegisterProcessorAsync_ValidSubProcessor_ShouldSucceed()
    {
        var parent = CreateProcessor(id: "parent", depth: 0);
        await _registry.RegisterProcessorAsync(parent);

        var sub = CreateProcessor(id: "sub-1", parentProcessorId: "parent", depth: 1);
        var result = await _registry.RegisterProcessorAsync(sub);

        result.IsRight.Should().BeTrue();
        _registry.Count.Should().Be(2);
    }

    [Fact]
    public async Task RegisterProcessorAsync_NullProcessor_ShouldThrow()
    {
        var act = async () => await _registry.RegisterProcessorAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region GetProcessorAsync

    [Fact]
    public async Task GetProcessorAsync_Existing_ShouldReturnSome()
    {
        var processor = CreateProcessor();
        await _registry.RegisterProcessorAsync(processor);

        var result = await _registry.GetProcessorAsync("proc-1");

        result.IsRight.Should().BeTrue();
        result.Match(Right: opt => opt.IsSome, Left: _ => false).Should().BeTrue();
    }

    [Fact]
    public async Task GetProcessorAsync_NotExisting_ShouldReturnNone()
    {
        var result = await _registry.GetProcessorAsync("non-existent");

        result.IsRight.Should().BeTrue();
        result.Match(Right: opt => opt.IsNone, Left: _ => false).Should().BeTrue();
    }

    #endregion

    #region GetAllProcessorsAsync

    [Fact]
    public async Task GetAllProcessorsAsync_Empty_ShouldReturnEmptyList()
    {
        var result = await _registry.GetAllProcessorsAsync();

        result.IsRight.Should().BeTrue();
        result.Match(Right: list => list.Count, Left: _ => -1).Should().Be(0);
    }

    [Fact]
    public async Task GetAllProcessorsAsync_WithItems_ShouldReturnAll()
    {
        await _registry.RegisterProcessorAsync(CreateProcessor(id: "p1"));
        await _registry.RegisterProcessorAsync(CreateProcessor(id: "p2"));

        var result = await _registry.GetAllProcessorsAsync();

        result.IsRight.Should().BeTrue();
        result.Match(Right: list => list.Count, Left: _ => -1).Should().Be(2);
    }

    #endregion

    #region UpdateProcessorAsync

    [Fact]
    public async Task UpdateProcessorAsync_Existing_ShouldSucceed()
    {
        var processor = CreateProcessor();
        await _registry.RegisterProcessorAsync(processor);

        var updated = processor with { Name = "Updated Name" };
        var result = await _registry.UpdateProcessorAsync(updated);

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateProcessorAsync_NotExisting_ShouldReturnNotFoundError()
    {
        var processor = CreateProcessor(id: "non-existent");

        var result = await _registry.UpdateProcessorAsync(processor);

        result.IsLeft.Should().BeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).Should().Be(ProcessorAgreementErrors.NotFoundCode);
    }

    #endregion

    #region RemoveProcessorAsync

    [Fact]
    public async Task RemoveProcessorAsync_Existing_ShouldSucceed()
    {
        var processor = CreateProcessor();
        await _registry.RegisterProcessorAsync(processor);

        var result = await _registry.RemoveProcessorAsync("proc-1");

        result.IsRight.Should().BeTrue();
        _registry.Count.Should().Be(0);
    }

    [Fact]
    public async Task RemoveProcessorAsync_NotExisting_ShouldReturnNotFoundError()
    {
        var result = await _registry.RemoveProcessorAsync("non-existent");

        result.IsLeft.Should().BeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).Should().Be(ProcessorAgreementErrors.NotFoundCode);
    }

    #endregion

    #region GetSubProcessorsAsync

    [Fact]
    public async Task GetSubProcessorsAsync_ShouldReturnDirectChildrenOnly()
    {
        var parent = CreateProcessor(id: "parent", depth: 0);
        var child = CreateProcessor(id: "child", parentProcessorId: "parent", depth: 1);
        var grandchild = CreateProcessor(id: "grandchild", parentProcessorId: "child", depth: 2);

        await _registry.RegisterProcessorAsync(parent);
        await _registry.RegisterProcessorAsync(child);
        await _registry.RegisterProcessorAsync(grandchild);

        var result = await _registry.GetSubProcessorsAsync("parent");

        result.IsRight.Should().BeTrue();
        var children = result.Match(Right: list => list, Left: _ => []);
        children.Should().HaveCount(1);
        children[0].Id.Should().Be("child");
    }

    #endregion

    #region GetFullSubProcessorChainAsync

    [Fact]
    public async Task GetFullSubProcessorChainAsync_ShouldReturnFullBFSChain()
    {
        var parent = CreateProcessor(id: "root", depth: 0);
        var child1 = CreateProcessor(id: "child-1", parentProcessorId: "root", depth: 1);
        var child2 = CreateProcessor(id: "child-2", parentProcessorId: "root", depth: 1);
        var grandchild = CreateProcessor(id: "grandchild-1", parentProcessorId: "child-1", depth: 2);

        await _registry.RegisterProcessorAsync(parent);
        await _registry.RegisterProcessorAsync(child1);
        await _registry.RegisterProcessorAsync(child2);
        await _registry.RegisterProcessorAsync(grandchild);

        var result = await _registry.GetFullSubProcessorChainAsync("root");

        result.IsRight.Should().BeTrue();
        var chain = result.Match(Right: list => list, Left: _ => []);
        chain.Should().HaveCount(3);
        chain.Select(p => p.Id).Should().Contain("child-1")
            .And.Contain("child-2")
            .And.Contain("grandchild-1");
    }

    [Fact]
    public async Task GetFullSubProcessorChainAsync_NoChildren_ShouldReturnEmpty()
    {
        var parent = CreateProcessor(id: "lonely");
        await _registry.RegisterProcessorAsync(parent);

        var result = await _registry.GetFullSubProcessorChainAsync("lonely");

        result.IsRight.Should().BeTrue();
        result.Match(Right: list => list.Count, Left: _ => -1).Should().Be(0);
    }

    #endregion

    #region Internal Properties

    [Fact]
    public void MaxSubProcessorDepth_Default_ShouldBe5()
    {
        _registry.MaxSubProcessorDepth.Should().Be(InMemoryProcessorRegistry.DefaultMaxSubProcessorDepth);
        _registry.MaxSubProcessorDepth.Should().Be(5);
    }

    [Fact]
    public void Count_AfterClear_ShouldBeZero()
    {
        _registry.RegisterProcessorAsync(CreateProcessor()).AsTask().GetAwaiter().GetResult();
        _registry.Count.Should().Be(1);

        _registry.Clear();

        _registry.Count.Should().Be(0);
    }

    #endregion

    #region SubProcessorAuthorizationType Enum

    [Theory]
    [InlineData(SubProcessorAuthorizationType.Specific, 0)]
    [InlineData(SubProcessorAuthorizationType.General, 1)]
    public void SubProcessorAuthorizationType_ShouldHaveExpectedIntValue(SubProcessorAuthorizationType type, int expected)
    {
        ((int)type).Should().Be(expected);
    }

    #endregion
}
