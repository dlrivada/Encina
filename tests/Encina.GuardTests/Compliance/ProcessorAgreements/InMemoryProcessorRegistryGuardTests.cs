using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Guard tests for <see cref="InMemoryProcessorRegistry"/> to verify null parameter handling.
/// </summary>
public class InMemoryProcessorRegistryGuardTests
{
    private readonly InMemoryProcessorRegistry _registry = new(
        NullLogger<InMemoryProcessorRegistry>.Instance);

    #region Constructor Guards

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryProcessorRegistry(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region RegisterProcessorAsync Guards

    [Fact]
    public async Task RegisterProcessorAsync_NullProcessor_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await _registry.RegisterProcessorAsync(null!));
        ex.ParamName.ShouldBe("processor");
    }

    #endregion

    #region GetProcessorAsync Guards

    [Fact]
    public async Task GetProcessorAsync_NullProcessorId_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await _registry.GetProcessorAsync(null!));
        ex.ParamName.ShouldBe("processorId");
    }

    #endregion

    #region UpdateProcessorAsync Guards

    [Fact]
    public async Task UpdateProcessorAsync_NullProcessor_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await _registry.UpdateProcessorAsync(null!));
        ex.ParamName.ShouldBe("processor");
    }

    #endregion

    #region RemoveProcessorAsync Guards

    [Fact]
    public async Task RemoveProcessorAsync_NullProcessorId_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await _registry.RemoveProcessorAsync(null!));
        ex.ParamName.ShouldBe("processorId");
    }

    #endregion

    #region GetSubProcessorsAsync Guards

    [Fact]
    public async Task GetSubProcessorsAsync_NullProcessorId_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await _registry.GetSubProcessorsAsync(null!));
        ex.ParamName.ShouldBe("processorId");
    }

    #endregion

    #region GetFullSubProcessorChainAsync Guards

    [Fact]
    public async Task GetFullSubProcessorChainAsync_NullProcessorId_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await _registry.GetFullSubProcessorChainAsync(null!));
        ex.ParamName.ShouldBe("processorId");
    }

    #endregion
}
