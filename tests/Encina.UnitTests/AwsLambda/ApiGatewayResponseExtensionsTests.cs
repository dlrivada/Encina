using System.Net;
using Encina.AwsLambda;
using LanguageExt;

namespace Encina.UnitTests.AwsLambda;

public class ApiGatewayResponseExtensionsTests
{
    [Fact]
    public void ToApiGatewayResponse_WithSuccess_ReturnsOkResponse()
    {
        // Arrange
        var result = Either<EncinaError, TestOrder>.Right(new TestOrder { Id = "123", Name = "Test" });

        // Act
        var response = result.ToApiGatewayResponse();

        // Assert
        response.StatusCode.ShouldBe(200);
        response.Headers.ShouldContainKey("Content-Type");
        response.Headers["Content-Type"].ShouldBe("application/json");
        response.Body.ShouldContain("\"id\":\"123\"");
        response.Body.ShouldContain("\"name\":\"Test\"");
    }

    [Fact]
    public void ToApiGatewayResponse_WithCustomStatusCode_ReturnsCustomStatus()
    {
        // Arrange
        var result = Either<EncinaError, TestOrder>.Right(new TestOrder { Id = "123" });

        // Act
        var response = result.ToApiGatewayResponse(successStatusCode: 202);

        // Assert
        response.StatusCode.ShouldBe(202);
    }

    [Fact]
    public void ToApiGatewayResponse_WithError_ReturnsProblemDetails()
    {
        // Arrange
        var error = EncinaErrors.Create("validation.invalid_input", "Invalid input provided");
        var result = Either<EncinaError, TestOrder>.Left(error);

        // Act
        var response = result.ToApiGatewayResponse();

        // Assert
        response.StatusCode.ShouldBe(400);
        response.Headers["Content-Type"].ShouldBe("application/problem+json");
        response.Body.ShouldContain("\"status\":400");
        response.Body.ShouldContain("\"title\":\"Bad Request\"");
        response.Body.ShouldContain("Invalid input provided");
    }

    [Fact]
    public void ToProblemDetailsResponse_WithValidationError_Returns400()
    {
        // Arrange
        var error = EncinaErrors.Create("validation.required", "Field is required");

        // Act
        var response = error.ToProblemDetailsResponse();

        // Assert
        response.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public void ToProblemDetailsResponse_WithUnauthenticatedError_Returns401()
    {
        // Arrange
        var error = EncinaErrors.Create("authorization.unauthenticated", "Not authenticated");

        // Act
        var response = error.ToProblemDetailsResponse();

        // Assert
        response.StatusCode.ShouldBe((int)HttpStatusCode.Unauthorized);
    }

    [Fact]
    public void ToProblemDetailsResponse_WithAuthorizationError_Returns403()
    {
        // Arrange
        var error = EncinaErrors.Create("authorization.forbidden", "Access denied");

        // Act
        var response = error.ToProblemDetailsResponse();

        // Assert
        response.StatusCode.ShouldBe((int)HttpStatusCode.Forbidden);
    }

    [Fact]
    public void ToProblemDetailsResponse_WithNotFoundError_Returns404()
    {
        // Arrange
        var error = EncinaErrors.Create("order.not_found", "Order not found");

        // Act
        var response = error.ToProblemDetailsResponse();

        // Assert
        response.StatusCode.ShouldBe((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public void ToProblemDetailsResponse_WithConflictError_Returns409()
    {
        // Arrange
        var error = EncinaErrors.Create("order.already_exists", "Order already exists");

        // Act
        var response = error.ToProblemDetailsResponse();

        // Assert
        response.StatusCode.ShouldBe((int)HttpStatusCode.Conflict);
    }

    [Fact]
    public void ToProblemDetailsResponse_WithUnknownError_Returns500()
    {
        // Arrange
        var error = EncinaErrors.Create("unknown.error", "Something went wrong");

        // Act
        var response = error.ToProblemDetailsResponse();

        // Assert
        response.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public void ToProblemDetailsResponse_WithCustomStatusCode_UsesCustomStatus()
    {
        // Arrange
        var error = EncinaErrors.Create("validation.error", "Validation error");

        // Act
        var response = error.ToProblemDetailsResponse(statusCode: 422);

        // Assert
        response.StatusCode.ShouldBe(422);
    }

    [Fact]
    public void ToCreatedResponse_WithSuccess_Returns201WithLocation()
    {
        // Arrange
        var order = new TestOrder { Id = "123", Name = "Test" };
        var result = Either<EncinaError, TestOrder>.Right(order);

        // Act
        var response = result.ToCreatedResponse(o => $"/orders/{o.Id}");

        // Assert
        response.StatusCode.ShouldBe((int)HttpStatusCode.Created);
        response.Headers.ShouldContainKey("Location");
        response.Headers["Location"].ShouldBe("/orders/123");
        response.Headers["Content-Type"].ShouldBe("application/json");
    }

    [Fact]
    public void ToCreatedResponse_WithError_ReturnsProblemDetails()
    {
        // Arrange
        var error = EncinaErrors.Create("validation.error", "Validation failed");
        var result = Either<EncinaError, TestOrder>.Left(error);

        // Act
        var response = result.ToCreatedResponse(o => $"/orders/{o.Id}");

        // Assert
        response.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
        response.Headers["Content-Type"].ShouldBe("application/problem+json");
    }

    [Fact]
    public void ToNoContentResponse_WithSuccess_Returns204()
    {
        // Arrange
        var result = Either<EncinaError, Unit>.Right(Unit.Default);

        // Act
        var response = result.ToNoContentResponse();

        // Assert
        response.StatusCode.ShouldBe((int)HttpStatusCode.NoContent);
        response.Body.ShouldBeNullOrEmpty();
    }

    [Fact]
    public void ToNoContentResponse_WithError_ReturnsProblemDetails()
    {
        // Arrange
        var error = EncinaErrors.Create("order.not_found", "Order not found");
        var result = Either<EncinaError, Unit>.Left(error);

        // Act
        var response = result.ToNoContentResponse();

        // Assert
        response.StatusCode.ShouldBe((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public void ToHttpApiResponse_WithSuccess_ReturnsOkResponse()
    {
        // Arrange
        var result = Either<EncinaError, TestOrder>.Right(new TestOrder { Id = "123" });

        // Act
        var response = result.ToHttpApiResponse();

        // Assert
        response.StatusCode.ShouldBe(200);
        response.Headers.ShouldContainKey("Content-Type");
        response.Headers["Content-Type"].ShouldBe("application/json");
    }

    [Fact]
    public void ToHttpApiResponse_WithError_ReturnsProblemDetails()
    {
        // Arrange
        var error = EncinaErrors.Create("validation.error", "Error");
        var result = Either<EncinaError, TestOrder>.Left(error);

        // Act
        var response = result.ToHttpApiResponse();

        // Assert
        response.StatusCode.ShouldBe(400);
        response.Headers["Content-Type"].ShouldBe("application/problem+json");
    }

    [Theory]
    [InlineData("validation.field_required", 400)]
    [InlineData("encina.guard.validation_failed", 400)]
    [InlineData("authorization.unauthenticated", 401)]
    [InlineData("authorization.insufficient_permissions", 403)]
    [InlineData("entity.not_found", 404)]
    [InlineData("entity.missing", 404)]
    [InlineData("encina.request.handler_missing", 404)]
    [InlineData("entity.conflict", 409)]
    [InlineData("entity.already_exists", 409)]
    [InlineData("entity.duplicate", 409)]
    [InlineData("random.error", 500)]
    public void ErrorCodeMapping_MapsCorrectly(string errorCode, int expectedStatus)
    {
        // Arrange
        var error = EncinaErrors.Create(errorCode, "Test message");
        var result = Either<EncinaError, TestOrder>.Left(error);

        // Act
        var response = result.ToApiGatewayResponse();

        // Assert
        response.StatusCode.ShouldBe(expectedStatus);
    }

    private sealed class TestOrder
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
