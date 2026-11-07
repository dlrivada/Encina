using System.Text.Json;
using LanguageExt;
using LanguageExt.Common;
using Shouldly;
using Xunit;

namespace Encina.GuardTests.Cloud.AwsLambda;

/// <summary>
/// Guard tests for <see cref="ApiGatewayResponseExtensions"/>: invalid or boundary arguments are
/// rejected, or converted to a well-formed error response, never to a malformed one.
/// </summary>
public sealed class ApiGatewayResponseInvalidInputGuardTests
{
    private sealed record Payload(int Id);

    [Fact]
    public void ToCreatedResponse_NullLocationFactory_ThrowsEvenWhenResultIsLeft()
    {
        Either<EncinaError, Payload> result = EncinaErrors.Create("x.not_found", "missing");

        var action = () => result.ToCreatedResponse(null!);

        Should.Throw<ArgumentNullException>(action).ParamName.ShouldBe("locationFactory");
    }

    [Fact]
    public void ToCreatedResponse_LocationFactoryIsNotCalledForLeft()
    {
        Either<EncinaError, Payload> result = EncinaErrors.Create("x.not_found", "missing");
        var called = false;

        var response = result.ToCreatedResponse(_ =>
        {
            called = true;
            return "/x";
        });

        called.ShouldBeFalse();
        response.StatusCode.ShouldBe(404);
    }

    [Fact]
    public void ToCreatedResponse_ThrowingLocationFactory_PropagatesTheException()
    {
        Either<EncinaError, Payload> result = new Payload(1);

        var action = () => result.ToCreatedResponse<Payload>(_ => throw new InvalidOperationException("boom"));

        Should.Throw<InvalidOperationException>(action);
    }

    [Fact]
    public void ToCreatedResponse_ValidFactory_SetsLocationHeader()
    {
        Either<EncinaError, Payload> result = new Payload(7);

        var response = result.ToCreatedResponse(p => $"/items/{p.Id}");

        response.StatusCode.ShouldBe(201);
        response.Headers["Location"].ShouldBe("/items/7");
    }

    [Fact]
    public void ToApiGatewayResponse_DefaultEitherInBottomState_Throws()
    {
        Either<EncinaError, Payload> result = default;

        var action = () => result.ToApiGatewayResponse();

        Should.Throw<BottomException>(action);
    }

    [Fact]
    public void ToHttpApiResponse_DefaultEitherInBottomState_Throws()
    {
        Either<EncinaError, Payload> result = default;

        var action = () => result.ToHttpApiResponse();

        Should.Throw<BottomException>(action);
    }

    [Fact]
    public void ToNoContentResponse_DefaultEitherInBottomState_Throws()
    {
        Either<EncinaError, Unit> result = default;

        var action = () => result.ToNoContentResponse();

        Should.Throw<BottomException>(action);
    }

    [Theory]
    [InlineData(null, 500, "Internal Server Error")]
    [InlineData(0, 0, "An error occurred")]
    [InlineData(-1, -1, "An error occurred")]
    [InlineData(418, 418, "An error occurred")]
    [InlineData(404, 404, "Not Found")]
    public void ToProblemDetailsResponse_UnusualStatusCode_StillProducesProblemJson(int? statusCode, int expectedStatus, string expectedTitle)
    {
        var error = EncinaErrors.Create("unmapped.code", "detail");

        var response = error.ToProblemDetailsResponse(statusCode);

        response.StatusCode.ShouldBe(expectedStatus);
        response.Headers["Content-Type"].ShouldBe("application/problem+json");
        using var body = JsonDocument.Parse(response.Body);
        body.RootElement.GetProperty("title").GetString().ShouldBe(expectedTitle);
        body.RootElement.GetProperty("status").GetInt32().ShouldBe(expectedStatus);
    }

    [Theory]
    [InlineData("validation.failed", 400)]
    [InlineData("VALIDATION.FAILED", 400)]
    [InlineData("encina.guard.validation_failed", 400)]
    [InlineData("encina.authorization.unauthenticated", 401)]
    [InlineData("encina.authorization.denied", 403)]
    [InlineData("order.not_found", 404)]
    [InlineData("order.missing", 404)]
    [InlineData("encina.request.handler_missing", 404)]
    [InlineData("order.conflict", 409)]
    [InlineData("order.already_exists", 409)]
    [InlineData("order.duplicate", 409)]
    [InlineData("", 500)]
    [InlineData("something.else", 500)]
    public void ToProblemDetailsResponse_ErrorCode_MapsToExpectedStatus(string code, int expectedStatus)
    {
        var error = EncinaErrors.Create(code, "m");

        ApiGatewayResponseExtensions.ToProblemDetailsResponse(error).StatusCode.ShouldBe(expectedStatus);
    }

    [Fact]
    public void ToProblemDetailsResponse_EmptyErrorCode_OmitsErrorCodeExtension()
    {
        var error = EncinaErrors.Create(string.Empty, "m");

        var response = error.ToProblemDetailsResponse();

        response.Body.ShouldNotContain("errorCode");
    }

    [Fact]
    public void ToHttpApiResponse_LeftWithExplicitStatus_UsesIt()
    {
        Either<EncinaError, Payload> result = EncinaErrors.Create("order.conflict", "m");

        var response = result.ToHttpApiResponse(errorStatusCode: 422);

        response.StatusCode.ShouldBe(422);
        response.Headers["Content-Type"].ShouldBe("application/problem+json");
        response.Body.ShouldContain("order.conflict");
    }

    [Fact]
    public void ToHttpApiResponse_LeftWithoutCode_OmitsErrorCodeExtension()
    {
        Either<EncinaError, Payload> result = EncinaErrors.Create(string.Empty, "m");

        var response = result.ToHttpApiResponse();

        response.StatusCode.ShouldBe(500);
        response.Body.ShouldNotContain("errorCode");
    }

    [Fact]
    public void ToHttpApiResponse_RightWithCustomStatus_KeepsItAndSerializesCamelCase()
    {
        Either<EncinaError, Payload> result = new Payload(3);

        var response = result.ToHttpApiResponse(successStatusCode: 202);

        response.StatusCode.ShouldBe(202);
        response.Body.ShouldBe("{\"id\":3}");
    }

    [Fact]
    public void ToApiGatewayResponse_LeftWithExplicitStatus_UsesIt()
    {
        Either<EncinaError, Payload> result = EncinaErrors.Create("order.conflict", "m");

        var response = result.ToApiGatewayResponse(errorStatusCode: 422);

        response.StatusCode.ShouldBe(422);
    }

    [Fact]
    public void ToApiGatewayResponse_RightWithCustomStatus_KeepsIt()
    {
        Either<EncinaError, Payload> result = new Payload(3);

        var response = result.ToApiGatewayResponse(successStatusCode: 202);

        response.StatusCode.ShouldBe(202);
        response.Body.ShouldBe("{\"id\":3}");
    }

    [Fact]
    public void ToNoContentResponse_LeftBecomesProblemResponse()
    {
        Either<EncinaError, Unit> result = EncinaErrors.Create("order.not_found", "m");

        var response = result.ToNoContentResponse();

        response.StatusCode.ShouldBe(404);
    }

    [Fact]
    public void ToNoContentResponse_RightHasNoBody()
    {
        Either<EncinaError, Unit> result = Unit.Default;

        var response = result.ToNoContentResponse();

        response.StatusCode.ShouldBe(204);
        response.Body.ShouldBeNull();
    }
}
