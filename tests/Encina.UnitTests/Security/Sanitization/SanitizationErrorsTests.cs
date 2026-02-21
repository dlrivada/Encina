using Encina.Security.Sanitization;
using FluentAssertions;

namespace Encina.UnitTests.Security.Sanitization;

public sealed class SanitizationErrorsTests
{
    #region Constants

    [Fact]
    public void ProfileNotFoundCode_HasExpectedValue()
    {
        SanitizationErrors.ProfileNotFoundCode.Should().Be("sanitization.profile_not_found");
    }

    [Fact]
    public void PropertyErrorCode_HasExpectedValue()
    {
        SanitizationErrors.PropertyErrorCode.Should().Be("sanitization.property_error");
    }

    #endregion

    #region ProfileNotFound

    [Fact]
    public void ProfileNotFound_ReturnsErrorWithCode()
    {
        var error = SanitizationErrors.ProfileNotFound("BlogPost");

        error.GetCode().IfNone(string.Empty).Should().Be(SanitizationErrors.ProfileNotFoundCode);
    }

    [Fact]
    public void ProfileNotFound_ReturnsErrorWithProfileNameInMessage()
    {
        var error = SanitizationErrors.ProfileNotFound("BlogPost");

        error.Message.Should().Contain("BlogPost");
    }

    [Fact]
    public void ProfileNotFound_ReturnsErrorWithMetadata()
    {
        var error = SanitizationErrors.ProfileNotFound("CustomProfile");
        var details = error.GetDetails();

        details.Should().ContainKey("profileName");
        details["profileName"].Should().Be("CustomProfile");
        details.Should().ContainKey("stage");
        details["stage"].Should().Be("sanitization");
    }

    #endregion

    #region PropertyError

    [Fact]
    public void PropertyError_ReturnsErrorWithCode()
    {
        var error = SanitizationErrors.PropertyError("Title");

        error.GetCode().IfNone(string.Empty).Should().Be(SanitizationErrors.PropertyErrorCode);
    }

    [Fact]
    public void PropertyError_ReturnsErrorWithPropertyNameInMessage()
    {
        var error = SanitizationErrors.PropertyError("Title");

        error.Message.Should().Contain("Title");
    }

    [Fact]
    public void PropertyError_WithException_IncludesException()
    {
        var ex = new InvalidOperationException("test error");

        var error = SanitizationErrors.PropertyError("Title", ex);

        error.Exception.IsSome.Should().BeTrue();
    }

    [Fact]
    public void PropertyError_WithoutException_HasNoInnerException()
    {
        var error = SanitizationErrors.PropertyError("Title");

        // EncinaErrors.Create always wraps in an EncinaException,
        // so Exception is always Some. Verify the inner exception is null.
        error.Exception.IfNone(() => null!).InnerException.Should().BeNull();
    }

    [Fact]
    public void PropertyError_ReturnsErrorWithMetadata()
    {
        var error = SanitizationErrors.PropertyError("Content");
        var details = error.GetDetails();

        details.Should().ContainKey("propertyName");
        details["propertyName"].Should().Be("Content");
        details.Should().ContainKey("stage");
        details["stage"].Should().Be("sanitization");
    }

    #endregion
}
