using System.Net;

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
        var statusCode = HttpResponseDataExtensions.MapErrorCodeToStatusCode(error);

        // Assert
        statusCode.ShouldBe((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public void MapErrorCodeToStatusCode_GuardValidationFailed_Returns400()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "encina.guard.validation_failed",
            message: "Guard validation failed");

        // Act
        var statusCode = HttpResponseDataExtensions.MapErrorCodeToStatusCode(error);

        // Assert
        statusCode.ShouldBe((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public void MapErrorCodeToStatusCode_UnauthenticatedError_Returns401()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "authorization.unauthenticated",
            message: "Authentication required");

        // Act
        var statusCode = HttpResponseDataExtensions.MapErrorCodeToStatusCode(error);

        // Assert
        statusCode.ShouldBe((int)HttpStatusCode.Unauthorized);
    }

    [Fact]
    public void MapErrorCodeToStatusCode_AuthorizationError_Returns403()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "authorization.insufficient_roles",
            message: "Insufficient permissions");

        // Act
        var statusCode = HttpResponseDataExtensions.MapErrorCodeToStatusCode(error);

        // Assert
        statusCode.ShouldBe((int)HttpStatusCode.Forbidden);
    }

    [Fact]
    public void MapErrorCodeToStatusCode_NotFoundError_Returns404()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "user.not_found",
            message: "User not found");

        // Act
        var statusCode = HttpResponseDataExtensions.MapErrorCodeToStatusCode(error);

        // Assert
        statusCode.ShouldBe((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public void MapErrorCodeToStatusCode_HandlerMissingError_Returns404()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "encina.request.handler_missing",
            message: "No handler found");

        // Act
        var statusCode = HttpResponseDataExtensions.MapErrorCodeToStatusCode(error);

        // Assert
        statusCode.ShouldBe((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public void MapErrorCodeToStatusCode_MissingError_Returns404()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "resource.missing",
            message: "Resource is missing");

        // Act
        var statusCode = HttpResponseDataExtensions.MapErrorCodeToStatusCode(error);

        // Assert
        statusCode.ShouldBe((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public void MapErrorCodeToStatusCode_ConflictError_Returns409()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "user.conflict",
            message: "User conflict");

        // Act
        var statusCode = HttpResponseDataExtensions.MapErrorCodeToStatusCode(error);

        // Assert
        statusCode.ShouldBe((int)HttpStatusCode.Conflict);
    }

    [Fact]
    public void MapErrorCodeToStatusCode_AlreadyExistsError_Returns409()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "email.already_exists",
            message: "Email already exists");

        // Act
        var statusCode = HttpResponseDataExtensions.MapErrorCodeToStatusCode(error);

        // Assert
        statusCode.ShouldBe((int)HttpStatusCode.Conflict);
    }

    [Fact]
    public void MapErrorCodeToStatusCode_DuplicateError_Returns409()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "username.duplicate",
            message: "Username is duplicate");

        // Act
        var statusCode = HttpResponseDataExtensions.MapErrorCodeToStatusCode(error);

        // Assert
        statusCode.ShouldBe((int)HttpStatusCode.Conflict);
    }

    [Fact]
    public void MapErrorCodeToStatusCode_UnknownError_Returns500()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "unknown.error",
            message: "Something went wrong");

        // Act
        var statusCode = HttpResponseDataExtensions.MapErrorCodeToStatusCode(error);

        // Assert
        statusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public void MapErrorCodeToStatusCode_ErrorWithNoCode_Returns500()
    {
        // Arrange
        var error = EncinaErrors.Create(code: "", message: "Something went wrong");

        // Act
        var statusCode = HttpResponseDataExtensions.MapErrorCodeToStatusCode(error);

        // Assert
        statusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
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
        var statusCode = HttpResponseDataExtensions.MapErrorCodeToStatusCode(error);

        // Assert
        statusCode.ShouldBe((int)HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("authorization.policy_failed")]
    [InlineData("authorization.role_required")]
    public void MapErrorCodeToStatusCode_AuthorizationErrors_Return403(string errorCode)
    {
        // Arrange
        var error = EncinaErrors.Create(code: errorCode, message: "Authorization failed");

        // Act
        var statusCode = HttpResponseDataExtensions.MapErrorCodeToStatusCode(error);

        // Assert
        statusCode.ShouldBe((int)HttpStatusCode.Forbidden);
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
        var statusCode = HttpResponseDataExtensions.MapErrorCodeToStatusCode(error);

        // Assert
        statusCode.ShouldBe((int)HttpStatusCode.NotFound);
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
        var statusCode = HttpResponseDataExtensions.MapErrorCodeToStatusCode(error);

        // Assert
        statusCode.ShouldBe((int)HttpStatusCode.Conflict);
    }

    [Fact]
    public void GetTitle_Returns400Title()
    {
        // Act
        var title = HttpResponseDataExtensions.GetTitle(400);

        // Assert
        title.ShouldBe("Bad Request");
    }

    [Fact]
    public void GetTitle_Returns401Title()
    {
        // Act
        var title = HttpResponseDataExtensions.GetTitle(401);

        // Assert
        title.ShouldBe("Unauthorized");
    }

    [Fact]
    public void GetTitle_Returns403Title()
    {
        // Act
        var title = HttpResponseDataExtensions.GetTitle(403);

        // Assert
        title.ShouldBe("Forbidden");
    }

    [Fact]
    public void GetTitle_Returns404Title()
    {
        // Act
        var title = HttpResponseDataExtensions.GetTitle(404);

        // Assert
        title.ShouldBe("Not Found");
    }

    [Fact]
    public void GetTitle_Returns409Title()
    {
        // Act
        var title = HttpResponseDataExtensions.GetTitle(409);

        // Assert
        title.ShouldBe("Conflict");
    }

    [Fact]
    public void GetTitle_Returns500Title()
    {
        // Act
        var title = HttpResponseDataExtensions.GetTitle(500);

        // Assert
        title.ShouldBe("Internal Server Error");
    }

    [Fact]
    public void GetTitle_ReturnsDefaultTitle_ForUnknownStatusCode()
    {
        // Act
        var title = HttpResponseDataExtensions.GetTitle(418); // I'm a teapot

        // Assert
        title.ShouldBe("An error occurred");
    }
}
