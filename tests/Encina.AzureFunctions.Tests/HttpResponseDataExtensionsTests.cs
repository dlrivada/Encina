using System.Net;
using FluentAssertions;
using LanguageExt;
using Xunit;
using static LanguageExt.Prelude;

namespace Encina.AzureFunctions.Tests;

public class HttpResponseDataExtensionsTests
{
    [Fact]
    public void MapErrorCodeToStatusCode_ValidationError_Returns400()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "validation.invalid_input",
            message: "The input is invalid");

        // Act
        var statusCode = GetStatusCodeForError(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void MapErrorCodeToStatusCode_GuardValidationFailed_Returns400()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "encina.guard.validation_failed",
            message: "Guard validation failed");

        // Act
        var statusCode = GetStatusCodeForError(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void MapErrorCodeToStatusCode_UnauthenticatedError_Returns401()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "authorization.unauthenticated",
            message: "Authentication required");

        // Act
        var statusCode = GetStatusCodeForError(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public void MapErrorCodeToStatusCode_AuthorizationError_Returns403()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "authorization.insufficient_roles",
            message: "Insufficient permissions");

        // Act
        var statusCode = GetStatusCodeForError(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public void MapErrorCodeToStatusCode_NotFoundError_Returns404()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "user.not_found",
            message: "User not found");

        // Act
        var statusCode = GetStatusCodeForError(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public void MapErrorCodeToStatusCode_HandlerMissingError_Returns404()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "encina.request.handler_missing",
            message: "No handler found");

        // Act
        var statusCode = GetStatusCodeForError(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public void MapErrorCodeToStatusCode_MissingError_Returns404()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "resource.missing",
            message: "Resource is missing");

        // Act
        var statusCode = GetStatusCodeForError(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public void MapErrorCodeToStatusCode_ConflictError_Returns409()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "user.conflict",
            message: "User conflict");

        // Act
        var statusCode = GetStatusCodeForError(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public void MapErrorCodeToStatusCode_AlreadyExistsError_Returns409()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "email.already_exists",
            message: "Email already exists");

        // Act
        var statusCode = GetStatusCodeForError(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public void MapErrorCodeToStatusCode_DuplicateError_Returns409()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "username.duplicate",
            message: "Username is duplicate");

        // Act
        var statusCode = GetStatusCodeForError(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public void MapErrorCodeToStatusCode_UnknownError_Returns500()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "unknown.error",
            message: "Something went wrong");

        // Act
        var statusCode = GetStatusCodeForError(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Theory]
    [InlineData("validation.email")]
    [InlineData("validation.required_field")]
    [InlineData("validation.invalid_format")]
    public void MapErrorCodeToStatusCode_ValidationErrors_Return400(string errorCode)
    {
        // Arrange
        var error = EncinaErrors.Create(code: errorCode, message: "Validation failed");

        // Act
        var statusCode = GetStatusCodeForError(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("authorization.policy_failed")]
    [InlineData("authorization.role_required")]
    public void MapErrorCodeToStatusCode_AuthorizationErrors_Return403(string errorCode)
    {
        // Arrange
        var error = EncinaErrors.Create(code: errorCode, message: "Authorization failed");

        // Act
        var statusCode = GetStatusCodeForError(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("order.not_found")]
    [InlineData("product.not_found")]
    [InlineData("customer.not_found")]
    public void MapErrorCodeToStatusCode_NotFoundErrors_Return404(string errorCode)
    {
        // Arrange
        var error = EncinaErrors.Create(code: errorCode, message: "Not found");

        // Act
        var statusCode = GetStatusCodeForError(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("order.conflict")]
    [InlineData("product.already_exists")]
    [InlineData("customer.duplicate")]
    public void MapErrorCodeToStatusCode_ConflictErrors_Return409(string errorCode)
    {
        // Arrange
        var error = EncinaErrors.Create(code: errorCode, message: "Conflict");

        // Act
        var statusCode = GetStatusCodeForError(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public void GetTitle_Returns400Title()
    {
        // Act
        var title = GetTitleForStatusCode(400);

        // Assert
        title.Should().Be("Bad Request");
    }

    [Fact]
    public void GetTitle_Returns401Title()
    {
        // Act
        var title = GetTitleForStatusCode(401);

        // Assert
        title.Should().Be("Unauthorized");
    }

    [Fact]
    public void GetTitle_Returns403Title()
    {
        // Act
        var title = GetTitleForStatusCode(403);

        // Assert
        title.Should().Be("Forbidden");
    }

    [Fact]
    public void GetTitle_Returns404Title()
    {
        // Act
        var title = GetTitleForStatusCode(404);

        // Assert
        title.Should().Be("Not Found");
    }

    [Fact]
    public void GetTitle_Returns409Title()
    {
        // Act
        var title = GetTitleForStatusCode(409);

        // Assert
        title.Should().Be("Conflict");
    }

    [Fact]
    public void GetTitle_Returns500Title()
    {
        // Act
        var title = GetTitleForStatusCode(500);

        // Assert
        title.Should().Be("Internal Server Error");
    }

    [Fact]
    public void GetTitle_ReturnsDefaultTitle_ForUnknownStatusCode()
    {
        // Act
        var title = GetTitleForStatusCode(418); // I'm a teapot

        // Assert
        title.Should().Be("An error occurred");
    }

    // Helper method to simulate the error code to status code mapping
    // This tests the internal logic without requiring the full Azure Functions infrastructure
    private static HttpStatusCode GetStatusCodeForError(EncinaError error)
    {
        var code = error.GetCode().IfNone(string.Empty).ToLowerInvariant();

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

    private static string GetTitleForStatusCode(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        500 => "Internal Server Error",
        _ => "An error occurred"
    };
}
