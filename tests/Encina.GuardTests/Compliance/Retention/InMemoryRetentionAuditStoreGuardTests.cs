using Encina.Compliance.Retention.InMemory;
using Encina.Compliance.Retention.Model;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="InMemoryRetentionAuditStore"/> to verify null parameter handling.
/// </summary>
public class InMemoryRetentionAuditStoreGuardTests
{
    private readonly InMemoryRetentionAuditStore _store = new(NullLogger<InMemoryRetentionAuditStore>.Instance);

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryRetentionAuditStore(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region RecordAsync Guards

    /// <summary>
    /// Verifies that RecordAsync throws ArgumentNullException when entry is null.
    /// </summary>
    [Fact]
    public async Task RecordAsync_NullEntry_ThrowsArgumentNullException()
    {
        var act = async () => await _store.RecordAsync(null!);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("entry");
    }

    #endregion

    #region GetByEntityIdAsync Guards

    /// <summary>
    /// Verifies that GetByEntityIdAsync throws ArgumentException when entityId is null.
    /// </summary>
    [Fact]
    public async Task GetByEntityIdAsync_NullEntityId_ThrowsArgumentException()
    {
        var act = async () => await _store.GetByEntityIdAsync(null!);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("entityId");
    }

    /// <summary>
    /// Verifies that GetByEntityIdAsync throws ArgumentException when entityId is empty.
    /// </summary>
    [Fact]
    public async Task GetByEntityIdAsync_EmptyEntityId_ThrowsArgumentException()
    {
        var act = async () => await _store.GetByEntityIdAsync(string.Empty);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("entityId");
    }

    /// <summary>
    /// Verifies that GetByEntityIdAsync throws ArgumentException when entityId is whitespace.
    /// </summary>
    [Fact]
    public async Task GetByEntityIdAsync_WhitespaceEntityId_ThrowsArgumentException()
    {
        var act = async () => await _store.GetByEntityIdAsync(" ");
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("entityId");
    }

    #endregion
}
