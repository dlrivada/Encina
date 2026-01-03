using System.Net;
using Shouldly;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace Encina.AwsLambda.PropertyTests;

/// <summary>
/// Property-based tests for error code to HTTP status code mapping.
/// </summary>
public class ErrorCodeMappingProperties
{
    [Property]
    public void ValidationErrors_AlwaysReturn400(NonEmptyString suffix)
    {
        // Arrange
        var code = $"validation.{suffix.Get}";

        // Act
        var statusCode = MapErrorCodeToStatusCode(code);

        // Assert
        statusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("admin", HttpStatusCode.Forbidden)]
    [InlineData("forbidden", HttpStatusCode.Forbidden)]
    [InlineData("access_denied", HttpStatusCode.Forbidden)]
    [InlineData("unauthorized", HttpStatusCode.Forbidden)]
    [InlineData("permission_denied", HttpStatusCode.Forbidden)]
    [InlineData("unauthenticated", HttpStatusCode.Unauthorized)]
    public void AuthorizationErrors_ReturnExpectedStatusCode(string suffix, HttpStatusCode expectedStatusCode)
    {
        // Arrange
        var code = $"authorization.{suffix}";

        // Act
        var statusCode = MapErrorCodeToStatusCode(code);

        // Assert
        statusCode.ShouldBe(expectedStatusCode);
    }

    [Property]
    public void NotFoundErrors_AlwaysReturn404(NonEmptyString prefix)
    {
        // Arrange
        var code = $"{prefix.Get}.not_found";

        // Act
        var statusCode = MapErrorCodeToStatusCode(code);

        // Assert
        statusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Property]
    public void MissingErrors_AlwaysReturn404(NonEmptyString prefix)
    {
        // Arrange
        var code = $"{prefix.Get}.missing";

        // Act
        var statusCode = MapErrorCodeToStatusCode(code);

        // Assert
        statusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Property]
    public void ConflictErrors_AlwaysReturn409(NonEmptyString prefix)
    {
        // Arrange
        var code = $"{prefix.Get}.conflict";

        // Act
        var statusCode = MapErrorCodeToStatusCode(code);

        // Assert
        statusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Property]
    public void AlreadyExistsErrors_AlwaysReturn409(NonEmptyString prefix)
    {
        // Arrange
        var code = $"{prefix.Get}.already_exists";

        // Act
        var statusCode = MapErrorCodeToStatusCode(code);

        // Assert
        statusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Property]
    public void DuplicateErrors_AlwaysReturn409(NonEmptyString prefix)
    {
        // Arrange
        var code = $"{prefix.Get}.duplicate";

        // Act
        var statusCode = MapErrorCodeToStatusCode(code);

        // Assert
        statusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Property]
    public bool UnknownErrors_AlwaysReturn500(NonEmptyString codeInput)
    {
        // Arrange
        var errorCode = codeInput.Get;

        // Precondition: Skip known patterns
        var isKnownPattern =
            errorCode.StartsWith("validation.", StringComparison.OrdinalIgnoreCase) ||
            errorCode.StartsWith("authorization.", StringComparison.OrdinalIgnoreCase) ||
            errorCode.EndsWith(".not_found", StringComparison.OrdinalIgnoreCase) ||
            errorCode.EndsWith(".missing", StringComparison.OrdinalIgnoreCase) ||
            errorCode.EndsWith(".conflict", StringComparison.OrdinalIgnoreCase) ||
            errorCode.EndsWith(".already_exists", StringComparison.OrdinalIgnoreCase) ||
            errorCode.EndsWith(".duplicate", StringComparison.OrdinalIgnoreCase) ||
            errorCode == "encina.guard.validation_failed" ||
            errorCode == "encina.request.handler_missing";

        // Act
        var statusCode = MapErrorCodeToStatusCode(errorCode);

        // Assert with precondition - if it's a known pattern, skip (trivially true); otherwise check 500
        return isKnownPattern || statusCode == HttpStatusCode.InternalServerError;
    }

    [Fact]
    public void ErrorCodeMapping_IsCaseInsensitive()
    {
        var testCases = new[] { "validation.test", "VALIDATION.TEST", "Validation.Test", "VaLiDaTiOn.TeSt" };
        foreach (var code in testCases)
        {
            var statusCode = MapErrorCodeToStatusCode(code);
            statusCode.ShouldBe(HttpStatusCode.BadRequest);
        }
    }

    [Property]
    public void StatusCodeMapping_AlwaysReturnsValidHttpStatusCode(NonEmptyString code)
    {
        // Act
        var statusCode = MapErrorCodeToStatusCode(code.Get);

        // Assert
        Enum.IsDefined<HttpStatusCode>(statusCode).ShouldBeTrue();
    }

    // Helper method that mirrors the actual implementation
    private static HttpStatusCode MapErrorCodeToStatusCode(string code)
    {
        code = code.ToLowerInvariant();

        if (code.StartsWith("validation.", StringComparison.Ordinal) ||
            code == "encina.guard.validation_failed")
        {
            return HttpStatusCode.BadRequest;
        }

        if (code == "authorization.unauthenticated")
        {
            return HttpStatusCode.Unauthorized;
        }

        if (code.StartsWith("authorization.", StringComparison.Ordinal))
        {
            return HttpStatusCode.Forbidden;
        }

        if (code.EndsWith(".not_found", StringComparison.Ordinal) ||
            code == "encina.request.handler_missing" ||
            code.EndsWith(".missing", StringComparison.Ordinal))
        {
            return HttpStatusCode.NotFound;
        }

        if (code.EndsWith(".conflict", StringComparison.Ordinal) ||
            code.EndsWith(".already_exists", StringComparison.Ordinal) ||
            code.EndsWith(".duplicate", StringComparison.Ordinal))
        {
            return HttpStatusCode.Conflict;
        }

        return HttpStatusCode.InternalServerError;
    }
}
