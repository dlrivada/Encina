using Encina.Compliance.NIS2;

using Shouldly;

using LanguageExt;

namespace Encina.UnitTests.Compliance.NIS2;

/// <summary>
/// Unit tests for <see cref="DefaultMFAEnforcer"/>.
/// </summary>
public class DefaultMFAEnforcerTests
{
    private readonly DefaultMFAEnforcer _sut = new();

    #region IsMFAEnabledAsync

    [Fact]
    public async Task IsMFAEnabledAsync_ShouldAlwaysReturnTrue()
    {
        // Arrange
        var userId = "user-123";

        // Act
        var result = await _sut.IsMFAEnabledAsync(userId);

        // Assert
        result.IsRight.ShouldBeTrue();
        var isEnabled = result.Match(r => r, _ => false);
        isEnabled.ShouldBeTrue();
    }

    #endregion

    #region RequireMFAAsync

    [Fact]
    public async Task RequireMFAAsync_ShouldAlwaysReturnSuccess()
    {
        // Arrange
        var request = new SampleMFARequest();
        var context = RequestContext.CreateForTest();

        // Act
        var result = await _sut.RequireMFAAsync(request, context);

        // Assert
        result.IsRight.ShouldBeTrue();
        var unit = result.Match(r => r, _ => default);
        unit.ShouldBe(Unit.Default);
    }

    #endregion

    /// <summary>
    /// Sample request type for MFA testing.
    /// </summary>
    private sealed record SampleMFARequest;
}
