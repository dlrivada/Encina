using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

namespace Encina.GuardTests.Compliance.DPIA;

/// <summary>
/// Guard tests for <see cref="InMemoryDPIAAuditStore"/> to verify null parameter handling.
/// </summary>
public class InMemoryDPIAAuditStoreGuardTests
{
    private readonly InMemoryDPIAAuditStore _store = new(
        NullLogger<InMemoryDPIAAuditStore>.Instance);

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryDPIAAuditStore(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region RecordAuditEntryAsync Guards

    /// <summary>
    /// Verifies that RecordAuditEntryAsync throws ArgumentNullException when entry is null.
    /// </summary>
    [Fact]
    public async Task RecordAuditEntryAsync_NullEntry_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await _store.RecordAuditEntryAsync(null!));
        ex.ParamName.ShouldBe("entry");
    }

    #endregion
}
