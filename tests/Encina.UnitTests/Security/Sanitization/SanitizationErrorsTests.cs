using Encina.Security.Sanitization;
using Shouldly;

namespace Encina.UnitTests.Security.Sanitization;

public sealed class SanitizationErrorsTests
{
    #region Constants

    [Fact]
    public void ProfileNotFoundCode_HasExpectedValue()
    {
        SanitizationErrors.ProfileNotFoundCode.ShouldBe("sanitization.profile_not_found");
    }

    [Fact]
    public void PropertyErrorCode_HasExpectedValue()
    {
        SanitizationErrors.PropertyErrorCode.ShouldBe("sanitization.property_error");
    }

    #endregion

    #region ProfileNotFound

    [Fact]
    public void ProfileNotFound_ReturnsErrorWithCode()
    {
        var error = SanitizationErrors.ProfileNotFound("BlogPost");

        error.GetCode().IfNone(string.Empty).ShouldBe(SanitizationErrors.ProfileNotFoundCode);
    }

    [Fact]
    public void ProfileNotFound_ReturnsErrorWithProfileNameInMessage()
    {
        var error = SanitizationErrors.ProfileNotFound("BlogPost");

        error.Message.ShouldContain("BlogPost");
    }

    [Fact]
    public void ProfileNotFound_ReturnsErrorWithMetadata()
    {
        var error = SanitizationErrors.ProfileNotFound("CustomProfile");
        var details = error.GetDetails();

        details.ShouldContainKey("profileName");
        details["profileName"].ShouldBe("CustomProfile");
        details.ShouldContainKey("stage");
        details["stage"].ShouldBe("sanitization");
    }

    #endregion

    #region PropertyError

    [Fact]
    public void PropertyError_ReturnsErrorWithCode()
    {
        var error = SanitizationErrors.PropertyError("Title");

        error.GetCode().IfNone(string.Empty).ShouldBe(SanitizationErrors.PropertyErrorCode);
    }

    [Fact]
    public void PropertyError_ReturnsErrorWithPropertyNameInMessage()
    {
        var error = SanitizationErrors.PropertyError("Title");

        error.Message.ShouldContain("Title");
    }

    [Fact]
    public void PropertyError_WithException_IncludesException()
    {
        var ex = new InvalidOperationException("test error");

        var error = SanitizationErrors.PropertyError("Title", ex);

        error.Exception.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void PropertyError_WithoutException_HasNoInnerException()
    {
        var error = SanitizationErrors.PropertyError("Title");

        // EncinaErrors.Create always wraps in an EncinaException,
        // so Exception is always Some. Verify the inner exception is null.
        error.Exception.IfNone(() => null!).InnerException.ShouldBeNull();
    }

    [Fact]
    public void PropertyError_ReturnsErrorWithMetadata()
    {
        var error = SanitizationErrors.PropertyError("Content");
        var details = error.GetDetails();

        details.ShouldContainKey("propertyName");
        details["propertyName"].ShouldBe("Content");
        details.ShouldContainKey("stage");
        details["stage"].ShouldBe("sanitization");
    }

    #endregion
}
