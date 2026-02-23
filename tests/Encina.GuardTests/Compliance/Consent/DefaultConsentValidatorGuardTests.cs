using Encina.Compliance.Consent;

namespace Encina.GuardTests.Compliance.Consent;

/// <summary>
/// Guard tests for <see cref="DefaultConsentValidator"/> to verify null and invalid parameter handling.
/// </summary>
public class DefaultConsentValidatorGuardTests
{
    private readonly IConsentStore _consentStore;
    private readonly IConsentVersionManager _versionManager;
    private readonly TimeProvider _timeProvider;

    public DefaultConsentValidatorGuardTests()
    {
        _consentStore = Substitute.For<IConsentStore>();
        _versionManager = Substitute.For<IConsentVersionManager>();
        _timeProvider = TimeProvider.System;
    }

    #region Constructor Guard Tests

    [Fact]
    public void Constructor_NullConsentStore_ThrowsArgumentNullException()
    {
        var act = () => new DefaultConsentValidator(null!, _versionManager, _timeProvider);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("consentStore");
    }

    [Fact]
    public void Constructor_NullVersionManager_ThrowsArgumentNullException()
    {
        var act = () => new DefaultConsentValidator(_consentStore, null!, _timeProvider);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("versionManager");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultConsentValidator(_consentStore, _versionManager, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    #endregion

    #region ValidateAsync Guard Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_InvalidSubjectId_ThrowsArgumentException(string? subjectId)
    {
        var validator = CreateValidator();

        var act = async () => await validator.ValidateAsync(subjectId!, ["marketing"]);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("subjectId");
    }

    [Fact]
    public async Task ValidateAsync_NullRequiredPurposes_ThrowsArgumentNullException()
    {
        var validator = CreateValidator();

        var act = async () => await validator.ValidateAsync("user-1", null!);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("requiredPurposes");
    }

    #endregion

    #region Helpers

    private DefaultConsentValidator CreateValidator()
    {
        return new DefaultConsentValidator(_consentStore, _versionManager, _timeProvider);
    }

    #endregion
}
