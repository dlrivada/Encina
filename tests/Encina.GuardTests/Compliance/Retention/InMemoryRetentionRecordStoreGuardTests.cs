using Encina.Compliance.Retention.InMemory;
using Encina.Compliance.Retention.Model;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="InMemoryRetentionRecordStore"/> to verify null parameter handling.
/// </summary>
public class InMemoryRetentionRecordStoreGuardTests
{
    private readonly InMemoryRetentionRecordStore _store = new(
        TimeProvider.System,
        NullLogger<InMemoryRetentionRecordStore>.Instance);

    private static readonly RetentionRecord ValidRecord = RetentionRecord.Create(
        entityId: "entity-123",
        dataCategory: "financial-records",
        createdAtUtc: DateTimeOffset.UtcNow,
        expiresAtUtc: DateTimeOffset.UtcNow.AddYears(7));

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when timeProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryRetentionRecordStore(
            null!,
            NullLogger<InMemoryRetentionRecordStore>.Instance);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryRetentionRecordStore(
            TimeProvider.System,
            null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region CreateAsync Guards

    /// <summary>
    /// Verifies that CreateAsync throws ArgumentNullException when record is null.
    /// </summary>
    [Fact]
    public async Task CreateAsync_NullRecord_ThrowsArgumentNullException()
    {
        var act = async () => await _store.CreateAsync(null!);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("record");
    }

    #endregion

    #region GetByIdAsync Guards

    /// <summary>
    /// Verifies that GetByIdAsync throws ArgumentException when recordId is null.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_NullRecordId_ThrowsArgumentException()
    {
        var act = async () => await _store.GetByIdAsync(null!);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("recordId");
    }

    /// <summary>
    /// Verifies that GetByIdAsync throws ArgumentException when recordId is empty.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_EmptyRecordId_ThrowsArgumentException()
    {
        var act = async () => await _store.GetByIdAsync(string.Empty);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("recordId");
    }

    /// <summary>
    /// Verifies that GetByIdAsync throws ArgumentException when recordId is whitespace.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WhitespaceRecordId_ThrowsArgumentException()
    {
        var act = async () => await _store.GetByIdAsync(" ");
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("recordId");
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

    #region UpdateStatusAsync Guards

    /// <summary>
    /// Verifies that UpdateStatusAsync throws ArgumentException when recordId is null.
    /// </summary>
    [Fact]
    public async Task UpdateStatusAsync_NullRecordId_ThrowsArgumentException()
    {
        var act = async () => await _store.UpdateStatusAsync(null!, RetentionStatus.Deleted);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("recordId");
    }

    /// <summary>
    /// Verifies that UpdateStatusAsync throws ArgumentException when recordId is empty.
    /// </summary>
    [Fact]
    public async Task UpdateStatusAsync_EmptyRecordId_ThrowsArgumentException()
    {
        var act = async () => await _store.UpdateStatusAsync(string.Empty, RetentionStatus.Deleted);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("recordId");
    }

    /// <summary>
    /// Verifies that UpdateStatusAsync throws ArgumentException when recordId is whitespace.
    /// </summary>
    [Fact]
    public async Task UpdateStatusAsync_WhitespaceRecordId_ThrowsArgumentException()
    {
        var act = async () => await _store.UpdateStatusAsync(" ", RetentionStatus.Deleted);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("recordId");
    }

    #endregion
}
