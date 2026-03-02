using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="DefaultLegalHoldManager"/> to verify null parameter handling.
/// </summary>
public class DefaultLegalHoldManagerGuardTests
{
    private readonly ILegalHoldStore _holdStore = Substitute.For<ILegalHoldStore>();
    private readonly IRetentionRecordStore _recordStore = Substitute.For<IRetentionRecordStore>();
    private readonly IRetentionAuditStore _auditStore = Substitute.For<IRetentionAuditStore>();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly ILogger<DefaultLegalHoldManager> _logger = NullLogger<DefaultLegalHoldManager>.Instance;

    private static readonly LegalHold ValidHold = LegalHold.Create(
        entityId: "entity-123",
        reason: "Pending audit");

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when holdStore is null.
    /// </summary>
    [Fact]
    public void Constructor_NullHoldStore_ThrowsArgumentNullException()
    {
        var act = () => new DefaultLegalHoldManager(null!, _recordStore, _auditStore, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("holdStore");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when recordStore is null.
    /// </summary>
    [Fact]
    public void Constructor_NullRecordStore_ThrowsArgumentNullException()
    {
        var act = () => new DefaultLegalHoldManager(_holdStore, null!, _auditStore, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("recordStore");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when auditStore is null.
    /// </summary>
    [Fact]
    public void Constructor_NullAuditStore_ThrowsArgumentNullException()
    {
        var act = () => new DefaultLegalHoldManager(_holdStore, _recordStore, null!, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("auditStore");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when timeProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultLegalHoldManager(_holdStore, _recordStore, _auditStore, null!, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultLegalHoldManager(_holdStore, _recordStore, _auditStore, _timeProvider, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region ApplyHoldAsync Guards

    /// <summary>
    /// Verifies that ApplyHoldAsync throws ArgumentException when entityId is null.
    /// </summary>
    [Fact]
    public async Task ApplyHoldAsync_NullEntityId_ThrowsArgumentException()
    {
        var instance = CreateInstance();

        var act = () => instance.ApplyHoldAsync(null!, ValidHold).AsTask();
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("entityId");
    }

    /// <summary>
    /// Verifies that ApplyHoldAsync throws ArgumentException when entityId is empty.
    /// </summary>
    [Fact]
    public async Task ApplyHoldAsync_EmptyEntityId_ThrowsArgumentException()
    {
        var instance = CreateInstance();

        var act = () => instance.ApplyHoldAsync(string.Empty, ValidHold).AsTask();
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("entityId");
    }

    /// <summary>
    /// Verifies that ApplyHoldAsync throws ArgumentException when entityId is whitespace.
    /// </summary>
    [Fact]
    public async Task ApplyHoldAsync_WhitespaceEntityId_ThrowsArgumentException()
    {
        var instance = CreateInstance();

        var act = () => instance.ApplyHoldAsync(" ", ValidHold).AsTask();
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("entityId");
    }

    /// <summary>
    /// Verifies that ApplyHoldAsync throws ArgumentNullException when hold is null.
    /// </summary>
    [Fact]
    public async Task ApplyHoldAsync_NullHold_ThrowsArgumentNullException()
    {
        var instance = CreateInstance();

        var act = () => instance.ApplyHoldAsync("entity-123", null!).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("hold");
    }

    #endregion

    #region ReleaseHoldAsync Guards

    /// <summary>
    /// Verifies that ReleaseHoldAsync throws ArgumentException when holdId is null.
    /// </summary>
    [Fact]
    public async Task ReleaseHoldAsync_NullHoldId_ThrowsArgumentException()
    {
        var instance = CreateInstance();

        var act = () => instance.ReleaseHoldAsync(null!, null).AsTask();
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("holdId");
    }

    /// <summary>
    /// Verifies that ReleaseHoldAsync throws ArgumentException when holdId is empty.
    /// </summary>
    [Fact]
    public async Task ReleaseHoldAsync_EmptyHoldId_ThrowsArgumentException()
    {
        var instance = CreateInstance();

        var act = () => instance.ReleaseHoldAsync(string.Empty, null).AsTask();
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("holdId");
    }

    /// <summary>
    /// Verifies that ReleaseHoldAsync throws ArgumentException when holdId is whitespace.
    /// </summary>
    [Fact]
    public async Task ReleaseHoldAsync_WhitespaceHoldId_ThrowsArgumentException()
    {
        var instance = CreateInstance();

        var act = () => instance.ReleaseHoldAsync(" ", null).AsTask();
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("holdId");
    }

    #endregion

    #region IsUnderHoldAsync Guards

    /// <summary>
    /// Verifies that IsUnderHoldAsync throws ArgumentException when entityId is null.
    /// </summary>
    [Fact]
    public async Task IsUnderHoldAsync_NullEntityId_ThrowsArgumentException()
    {
        var instance = CreateInstance();

        var act = () => instance.IsUnderHoldAsync(null!).AsTask();
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("entityId");
    }

    /// <summary>
    /// Verifies that IsUnderHoldAsync throws ArgumentException when entityId is empty.
    /// </summary>
    [Fact]
    public async Task IsUnderHoldAsync_EmptyEntityId_ThrowsArgumentException()
    {
        var instance = CreateInstance();

        var act = () => instance.IsUnderHoldAsync(string.Empty).AsTask();
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("entityId");
    }

    /// <summary>
    /// Verifies that IsUnderHoldAsync throws ArgumentException when entityId is whitespace.
    /// </summary>
    [Fact]
    public async Task IsUnderHoldAsync_WhitespaceEntityId_ThrowsArgumentException()
    {
        var instance = CreateInstance();

        var act = () => instance.IsUnderHoldAsync(" ").AsTask();
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("entityId");
    }

    #endregion

    private DefaultLegalHoldManager CreateInstance() =>
        new(_holdStore, _recordStore, _auditStore, _timeProvider, _logger);
}
