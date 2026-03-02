using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="DefaultRetentionPolicy"/> to verify null parameter handling.
/// </summary>
public class DefaultRetentionPolicyGuardTests
{
    private readonly IRetentionPolicyStore _policyStore = Substitute.For<IRetentionPolicyStore>();
    private readonly IRetentionRecordStore _recordStore = Substitute.For<IRetentionRecordStore>();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly IOptions<RetentionOptions> _options;
    private readonly ILogger<DefaultRetentionPolicy> _logger = NullLogger<DefaultRetentionPolicy>.Instance;

    public DefaultRetentionPolicyGuardTests()
    {
        _options = Substitute.For<IOptions<RetentionOptions>>();
        _options.Value.Returns(new RetentionOptions());
    }

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when policyStore is null.
    /// </summary>
    [Fact]
    public void Constructor_NullPolicyStore_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionPolicy(null!, _recordStore, _timeProvider, _options, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("policyStore");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when recordStore is null.
    /// </summary>
    [Fact]
    public void Constructor_NullRecordStore_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionPolicy(_policyStore, null!, _timeProvider, _options, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("recordStore");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when timeProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionPolicy(_policyStore, _recordStore, null!, _options, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionPolicy(_policyStore, _recordStore, _timeProvider, null!, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionPolicy(_policyStore, _recordStore, _timeProvider, _options, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region GetRetentionPeriodAsync Guards

    /// <summary>
    /// Verifies that GetRetentionPeriodAsync throws ArgumentException when dataCategory is null.
    /// </summary>
    [Fact]
    public async Task GetRetentionPeriodAsync_NullDataCategory_ThrowsArgumentException()
    {
        var instance = CreateInstance();

        var act = () => instance.GetRetentionPeriodAsync(null!).AsTask();
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("dataCategory");
    }

    /// <summary>
    /// Verifies that GetRetentionPeriodAsync throws ArgumentException when dataCategory is empty.
    /// </summary>
    [Fact]
    public async Task GetRetentionPeriodAsync_EmptyDataCategory_ThrowsArgumentException()
    {
        var instance = CreateInstance();

        var act = () => instance.GetRetentionPeriodAsync(string.Empty).AsTask();
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("dataCategory");
    }

    /// <summary>
    /// Verifies that GetRetentionPeriodAsync throws ArgumentException when dataCategory is whitespace.
    /// </summary>
    [Fact]
    public async Task GetRetentionPeriodAsync_WhitespaceDataCategory_ThrowsArgumentException()
    {
        var instance = CreateInstance();

        var act = () => instance.GetRetentionPeriodAsync(" ").AsTask();
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("dataCategory");
    }

    #endregion

    #region IsExpiredAsync Guards

    /// <summary>
    /// Verifies that IsExpiredAsync throws ArgumentException when entityId is null.
    /// </summary>
    [Fact]
    public async Task IsExpiredAsync_NullEntityId_ThrowsArgumentException()
    {
        var instance = CreateInstance();

        var act = () => instance.IsExpiredAsync(null!, "financial-records").AsTask();
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("entityId");
    }

    /// <summary>
    /// Verifies that IsExpiredAsync throws ArgumentException when entityId is empty.
    /// </summary>
    [Fact]
    public async Task IsExpiredAsync_EmptyEntityId_ThrowsArgumentException()
    {
        var instance = CreateInstance();

        var act = () => instance.IsExpiredAsync(string.Empty, "financial-records").AsTask();
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("entityId");
    }

    /// <summary>
    /// Verifies that IsExpiredAsync throws ArgumentException when entityId is whitespace.
    /// </summary>
    [Fact]
    public async Task IsExpiredAsync_WhitespaceEntityId_ThrowsArgumentException()
    {
        var instance = CreateInstance();

        var act = () => instance.IsExpiredAsync(" ", "financial-records").AsTask();
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("entityId");
    }

    /// <summary>
    /// Verifies that IsExpiredAsync throws ArgumentException when dataCategory is null.
    /// </summary>
    [Fact]
    public async Task IsExpiredAsync_NullDataCategory_ThrowsArgumentException()
    {
        var instance = CreateInstance();

        var act = () => instance.IsExpiredAsync("entity-123", null!).AsTask();
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("dataCategory");
    }

    /// <summary>
    /// Verifies that IsExpiredAsync throws ArgumentException when dataCategory is empty.
    /// </summary>
    [Fact]
    public async Task IsExpiredAsync_EmptyDataCategory_ThrowsArgumentException()
    {
        var instance = CreateInstance();

        var act = () => instance.IsExpiredAsync("entity-123", string.Empty).AsTask();
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("dataCategory");
    }

    /// <summary>
    /// Verifies that IsExpiredAsync throws ArgumentException when dataCategory is whitespace.
    /// </summary>
    [Fact]
    public async Task IsExpiredAsync_WhitespaceDataCategory_ThrowsArgumentException()
    {
        var instance = CreateInstance();

        var act = () => instance.IsExpiredAsync("entity-123", " ").AsTask();
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("dataCategory");
    }

    #endregion

    private DefaultRetentionPolicy CreateInstance() =>
        new(_policyStore, _recordStore, _timeProvider, _options, _logger);
}
