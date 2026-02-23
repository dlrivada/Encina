using Encina.Compliance.Consent;

namespace Encina.GuardTests.Compliance.Consent;

/// <summary>
/// Guard tests for <see cref="InMemoryConsentVersionManager"/> to verify null and invalid parameter handling.
/// </summary>
public class InMemoryConsentVersionManagerGuardTests
{
    private readonly IConsentStore _consentStore;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InMemoryConsentVersionManager> _logger;

    public InMemoryConsentVersionManagerGuardTests()
    {
        _consentStore = Substitute.For<IConsentStore>();
        _timeProvider = TimeProvider.System;
        _logger = NullLogger<InMemoryConsentVersionManager>.Instance;
    }

    #region Constructor Guard Tests

    [Fact]
    public void Constructor_NullConsentStore_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryConsentVersionManager(null!, _timeProvider, _logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("consentStore");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryConsentVersionManager(_consentStore, null!, _logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryConsentVersionManager(_consentStore, _timeProvider, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region GetCurrentVersionAsync Guard Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetCurrentVersionAsync_InvalidPurpose_ThrowsArgumentException(string? purpose)
    {
        var manager = CreateManager();

        var act = async () => await manager.GetCurrentVersionAsync(purpose!);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("purpose");
    }

    #endregion

    #region PublishNewVersionAsync Guard Tests

    [Fact]
    public async Task PublishNewVersionAsync_NullVersion_ThrowsArgumentNullException()
    {
        var manager = CreateManager();

        var act = async () => await manager.PublishNewVersionAsync(null!);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("version");
    }

    #endregion

    #region RequiresReconsentAsync Guard Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RequiresReconsentAsync_InvalidSubjectId_ThrowsArgumentException(string? subjectId)
    {
        var manager = CreateManager();

        var act = async () => await manager.RequiresReconsentAsync(subjectId!, "marketing");
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("subjectId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RequiresReconsentAsync_InvalidPurpose_ThrowsArgumentException(string? purpose)
    {
        var manager = CreateManager();

        var act = async () => await manager.RequiresReconsentAsync("user-1", purpose!);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("purpose");
    }

    #endregion

    #region Helpers

    private InMemoryConsentVersionManager CreateManager()
    {
        return new InMemoryConsentVersionManager(_consentStore, _timeProvider, _logger);
    }

    #endregion
}
