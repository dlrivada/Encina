using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Guard tests for <see cref="InMemoryProcessorAuditStore"/> to verify null parameter handling.
/// </summary>
public class InMemoryProcessorAuditStoreGuardTests
{
    private readonly InMemoryProcessorAuditStore _store = new(
        NullLogger<InMemoryProcessorAuditStore>.Instance);

    #region Constructor Guards

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryProcessorAuditStore(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region RecordAsync Guards

    [Fact]
    public async Task RecordAsync_NullEntry_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await _store.RecordAsync(null!));
        ex.ParamName.ShouldBe("entry");
    }

    #endregion

    #region GetAuditTrailAsync Guards

    [Fact]
    public async Task GetAuditTrailAsync_NullProcessorId_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await _store.GetAuditTrailAsync(null!));
        ex.ParamName.ShouldBe("processorId");
    }

    #endregion
}
