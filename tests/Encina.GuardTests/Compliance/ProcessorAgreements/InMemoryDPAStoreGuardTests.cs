using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Guard tests for <see cref="InMemoryDPAStore"/> to verify null parameter handling.
/// </summary>
public class InMemoryDPAStoreGuardTests
{
    private readonly InMemoryDPAStore _store = new(
        NullLogger<InMemoryDPAStore>.Instance);

    #region Constructor Guards

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryDPAStore(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region AddAsync Guards

    [Fact]
    public async Task AddAsync_NullAgreement_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await _store.AddAsync(null!));
        ex.ParamName.ShouldBe("agreement");
    }

    #endregion

    #region GetByIdAsync Guards

    [Fact]
    public async Task GetByIdAsync_NullDpaId_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await _store.GetByIdAsync(null!));
        ex.ParamName.ShouldBe("dpaId");
    }

    #endregion

    #region GetByProcessorIdAsync Guards

    [Fact]
    public async Task GetByProcessorIdAsync_NullProcessorId_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await _store.GetByProcessorIdAsync(null!));
        ex.ParamName.ShouldBe("processorId");
    }

    #endregion

    #region GetActiveByProcessorIdAsync Guards

    [Fact]
    public async Task GetActiveByProcessorIdAsync_NullProcessorId_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await _store.GetActiveByProcessorIdAsync(null!));
        ex.ParamName.ShouldBe("processorId");
    }

    #endregion

    #region UpdateAsync Guards

    [Fact]
    public async Task UpdateAsync_NullAgreement_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await _store.UpdateAsync(null!));
        ex.ParamName.ShouldBe("agreement");
    }

    #endregion
}
