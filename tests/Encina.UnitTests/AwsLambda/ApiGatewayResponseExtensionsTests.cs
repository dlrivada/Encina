using System.Net;
using System.Text.Json;
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
        response.Body.ShouldNotContain("Invalid input provided");
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
        var error = EncinaErrors.Create(EncinaErrorCodes.AuthorizationUnauthenticated, "Not authenticated");

        // Act
        var response = error.ToProblemDetailsResponse();

        // Assert
        response.StatusCode.ShouldBe((int)HttpStatusCode.Unauthorized);
    }

    [Fact]
    public void ToProblemDetailsResponse_WithAuthorizationError_Returns403()
    {
        // Arrange
        var error = EncinaErrors.Create(EncinaErrorCodes.AuthorizationForbidden, "Access denied");

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

    // The 14 categories of ADR-062 and the status each one answers.
    public static TheoryData<ErrorCategory, int, string> CategoryStatuses => new()
    {
        { ErrorCategory.Internal, 500, "Internal Server Error" },
        { ErrorCategory.Validation, 400, "Bad Request" },
        { ErrorCategory.InvalidRequest, 400, "Bad Request" },
        { ErrorCategory.Unauthenticated, 401, "Unauthorized" },
        { ErrorCategory.Forbidden, 403, "Forbidden" },
        { ErrorCategory.NotFound, 404, "Not Found" },
        { ErrorCategory.Conflict, 409, "Conflict" },
        { ErrorCategory.AlreadyExists, 409, "Conflict" },
        { ErrorCategory.FailedPrecondition, 400, "Bad Request" },
        { ErrorCategory.RateLimited, 429, "Too Many Requests" },
        { ErrorCategory.Unavailable, 503, "Service Unavailable" },
        { ErrorCategory.Timeout, 504, "Gateway Timeout" },
        { ErrorCategory.UpstreamFailure, 502, "Bad Gateway" },
        { ErrorCategory.Cancelled, 499, "Client Closed Request" },
    };

    [Theory]
    [MemberData(nameof(CategoryStatuses))]
    public void ToApiGatewayResponse_ForEveryCategory_AnswersTheCategoryStatusAndTitle(
        ErrorCategory category, int expectedStatus, string expectedTitle)
    {
        // Arrange
        var result = Either<EncinaError, TestOrder>.Left(EncinaErrors.Create("test.failure", category: category, "Test message"));

        // Act
        var response = result.ToApiGatewayResponse();

        // Assert
        response.StatusCode.ShouldBe(expectedStatus);
        response.Headers["Content-Type"].ShouldBe("application/problem+json");
        using var body = JsonDocument.Parse(response.Body);
        body.RootElement.GetProperty("status").GetInt32().ShouldBe(expectedStatus);
        body.RootElement.GetProperty("title").GetString().ShouldBe(expectedTitle);
    }

    [Theory]
    [MemberData(nameof(CategoryStatuses))]
    public void ToHttpApiResponse_ForEveryCategory_AnswersTheCategoryStatusAndTitle(
        ErrorCategory category, int expectedStatus, string expectedTitle)
    {
        // Arrange
        var result = Either<EncinaError, TestOrder>.Left(EncinaErrors.Create("test.failure", category: category, "Test message"));

        // Act
        var response = result.ToHttpApiResponse();

        // Assert
        response.StatusCode.ShouldBe(expectedStatus);
        response.Headers["Content-Type"].ShouldBe("application/problem+json");
        using var body = JsonDocument.Parse(response.Body);
        body.RootElement.GetProperty("status").GetInt32().ShouldBe(expectedStatus);
        body.RootElement.GetProperty("title").GetString().ShouldBe(expectedTitle);
    }

    [Fact]
    public void ToProblemDetailsResponse_ErrorWithoutCode_Returns500()
    {
        // Arrange
        var error = EncinaErrors.Create(string.Empty, "Test message");

        // Act
        var response = error.ToProblemDetailsResponse();

        // Assert
        response.StatusCode.ShouldBe(500);
    }

    [Fact]
    public void ToProblemDetailsResponse_ErrorCodeText_IsNeverParsedToChooseTheStatus()
    {
        // Arrange: the code looks like a not-found suffix but the category is Conflict
        var error = EncinaErrors.Create("order.not_found", category: ErrorCategory.Conflict, "Test message");

        // Act
        var response = error.ToProblemDetailsResponse();

        // Assert
        response.StatusCode.ShouldBe(409);
        response.Body.ShouldContain("\"errorCode\":\"order.not_found\"");
    }

    [Theory]
    [InlineData(ErrorCategory.RateLimited, 429)]
    [InlineData(ErrorCategory.Unavailable, 503)]
    public void ToProblemDetailsResponse_RetryAfterDetail_IsWrittenAsHeaderForBothShapes(ErrorCategory category, int status)
    {
        // Arrange
        var error = EncinaErrors.Create(
            "test.busy",
            category,
            "Busy",
            details: new Dictionary<string, object?> { [EncinaErrorMetadataKeys.RetryAfter] = TimeSpan.FromSeconds(30) });

        // Act
        var v1 = error.ToProblemDetailsResponse();
        var v2 = Either<EncinaError, TestOrder>.Left(error).ToHttpApiResponse();

        // Assert
        v1.StatusCode.ShouldBe(status);
        v1.Headers["Retry-After"].ShouldBe("30");
        v1.Headers["Content-Type"].ShouldBe("application/problem+json");
        v2.StatusCode.ShouldBe(status);
        v2.Headers["Retry-After"].ShouldBe("30");
        v2.Headers["Content-Type"].ShouldBe("application/problem+json");
    }

    [Fact]
    public void ToProblemDetailsResponse_RateLimitedWithoutRetryAfter_WritesNoRetryAfterHeader()
    {
        // Arrange
        var error = EncinaErrors.Create("test.busy", ErrorCategory.RateLimited, "Busy");

        // Act
        var response = error.ToProblemDetailsResponse();

        // Assert
        response.StatusCode.ShouldBe(429);
        response.Headers.ShouldNotContainKey("Retry-After");
    }

    [Fact]
    public void ToProblemDetailsResponse_Unauthenticated_WritesDefaultBearerChallengeForBothShapes()
    {
        // Arrange
        var error = EncinaErrors.Create("test.no_identity", ErrorCategory.Unauthenticated, "No identity");

        // Act
        var v1 = error.ToProblemDetailsResponse();
        var v2 = Either<EncinaError, TestOrder>.Left(error).ToHttpApiResponse();

        // Assert
        v1.Headers["WWW-Authenticate"].ShouldBe("Bearer");
        v2.Headers["WWW-Authenticate"].ShouldBe("Bearer");
    }

    [Fact]
    public void ToProblemDetailsResponse_Unauthenticated_WritesTheChallengeOfTheDetails()
    {
        // Arrange
        var error = EncinaErrors.Create(
            "test.no_identity",
            ErrorCategory.Unauthenticated,
            "No identity",
            details: new Dictionary<string, object?> { [EncinaErrorMetadataKeys.AuthenticateChallenge] = "Basic realm=\"orders\"" });

        // Act
        var response = error.ToProblemDetailsResponse();

        // Assert
        response.Headers["WWW-Authenticate"].ShouldBe("Basic realm=\"orders\"");
    }

    [Fact]
    public void ToProblemDetailsResponse_ExplicitStatusOverride_PassesThroughAndHeadersFollowTheEffectiveStatus()
    {
        // Arrange: a Validation error overridden to 503 gets the headers of 503, not those of 400
        var error = EncinaErrors.Create(
            "test.override",
            ErrorCategory.Validation,
            "Override",
            details: new Dictionary<string, object?> { [EncinaErrorMetadataKeys.RetryAfter] = TimeSpan.FromSeconds(5) });

        // Act
        var response = error.ToProblemDetailsResponse(statusCode: 503);
        var v2 = Either<EncinaError, TestOrder>.Left(error).ToHttpApiResponse(errorStatusCode: 401);

        // Assert
        response.StatusCode.ShouldBe(503);
        response.Headers["Retry-After"].ShouldBe("5");
        v2.StatusCode.ShouldBe(401);
        v2.Headers["WWW-Authenticate"].ShouldBe("Bearer");
        v2.Headers.ShouldNotContainKey("Retry-After");
    }

    [Fact]
    public void ToProblemDetailsResponse_NeverWritesTheErrorMessageOrAnExceptionMessage()
    {
        // Arrange
        const string Secret = "secret-connection-string-in-message";
        const string ExceptionSecret = "secret-in-exception-message";
        var error = EncinaErrors.Create(
            "test.failure",
            ErrorCategory.Internal,
            Secret,
            new InvalidOperationException(ExceptionSecret));

        // Act
        var v1 = error.ToProblemDetailsResponse();
        var v2 = Either<EncinaError, TestOrder>.Left(error).ToHttpApiResponse();

        // Assert
        foreach (var body in new[] { v1.Body, v2.Body })
        {
            body.ShouldNotContain(Secret);
            body.ShouldNotContain(ExceptionSecret);
            using var json = JsonDocument.Parse(body);
            json.RootElement.TryGetProperty("detail", out _).ShouldBeFalse();
        }
    }

    private sealed class TestOrder
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
