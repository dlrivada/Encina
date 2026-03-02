using Encina.Compliance.Retention.InMemory;
using Encina.Compliance.Retention.Model;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="InMemoryLegalHoldStore"/> to verify null parameter handling.
/// </summary>
public class InMemoryLegalHoldStoreGuardTests
{
    private readonly InMemoryLegalHoldStore _store = new(NullLogger<InMemoryLegalHoldStore>.Instance);

    private static readonly LegalHold ValidHold = LegalHold.Create(
        entityId: "entity-123",
        reason: "Pending audit");

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryLegalHoldStore(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region CreateAsync Guards

    /// <summary>
    /// Verifies that CreateAsync throws ArgumentNullException when hold is null.
    /// </summary>
    [Fact]
    public async Task CreateAsync_NullHold_ThrowsArgumentNullException()
    {
        var act = async () => await _store.CreateAsync(null!);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("hold");
    }

    #endregion

    #region GetByIdAsync Guards

    /// <summary>
    /// Verifies that GetByIdAsync throws ArgumentException when holdId is null.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_NullHoldId_ThrowsArgumentException()
    {
        var act = async () => await _store.GetByIdAsync(null!);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("holdId");
    }

    /// <summary>
    /// Verifies that GetByIdAsync throws ArgumentException when holdId is empty.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_EmptyHoldId_ThrowsArgumentException()
    {
        var act = async () => await _store.GetByIdAsync(string.Empty);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("holdId");
    }

    /// <summary>
    /// Verifies that GetByIdAsync throws ArgumentException when holdId is whitespace.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WhitespaceHoldId_ThrowsArgumentException()
    {
        var act = async () => await _store.GetByIdAsync(" ");
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("holdId");
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

    #region IsUnderHoldAsync Guards

    /// <summary>
    /// Verifies that IsUnderHoldAsync throws ArgumentException when entityId is null.
    /// </summary>
    [Fact]
    public async Task IsUnderHoldAsync_NullEntityId_ThrowsArgumentException()
    {
        var act = async () => await _store.IsUnderHoldAsync(null!);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("entityId");
    }

    /// <summary>
    /// Verifies that IsUnderHoldAsync throws ArgumentException when entityId is empty.
    /// </summary>
    [Fact]
    public async Task IsUnderHoldAsync_EmptyEntityId_ThrowsArgumentException()
    {
        var act = async () => await _store.IsUnderHoldAsync(string.Empty);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("entityId");
    }

    /// <summary>
    /// Verifies that IsUnderHoldAsync throws ArgumentException when entityId is whitespace.
    /// </summary>
    [Fact]
    public async Task IsUnderHoldAsync_WhitespaceEntityId_ThrowsArgumentException()
    {
        var act = async () => await _store.IsUnderHoldAsync(" ");
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("entityId");
    }

    #endregion

    #region ReleaseAsync Guards

    /// <summary>
    /// Verifies that ReleaseAsync throws ArgumentException when holdId is null.
    /// </summary>
    [Fact]
    public async Task ReleaseAsync_NullHoldId_ThrowsArgumentException()
    {
        var act = async () => await _store.ReleaseAsync(null!, null, DateTimeOffset.UtcNow);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("holdId");
    }

    /// <summary>
    /// Verifies that ReleaseAsync throws ArgumentException when holdId is empty.
    /// </summary>
    [Fact]
    public async Task ReleaseAsync_EmptyHoldId_ThrowsArgumentException()
    {
        var act = async () => await _store.ReleaseAsync(string.Empty, null, DateTimeOffset.UtcNow);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("holdId");
    }

    /// <summary>
    /// Verifies that ReleaseAsync throws ArgumentException when holdId is whitespace.
    /// </summary>
    [Fact]
    public async Task ReleaseAsync_WhitespaceHoldId_ThrowsArgumentException()
    {
        var act = async () => await _store.ReleaseAsync(" ", null, DateTimeOffset.UtcNow);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("holdId");
    }

    #endregion
}
