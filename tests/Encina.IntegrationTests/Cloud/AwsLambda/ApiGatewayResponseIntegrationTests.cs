using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Encina.AwsLambda;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static Encina.IntegrationTests.Cloud.AwsLambda.AwsLambdaTestSupport;

namespace Encina.IntegrationTests.Cloud.AwsLambda;

/// <summary>
/// Converts the results of the real Encina pipeline with <see cref="ApiGatewayResponseExtensions"/> and
/// checks the wire JSON the Lambda runtime serializer would return to API Gateway.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Cloud", "AwsLambda")]
public sealed class ApiGatewayResponseIntegrationTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ToApiGatewayResponse_ForASuccessfulCommand_ReturnsTheReceiptAsJson()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

        // Act
        var response = (await encina.Send(new PlaceOrder("o-1", 10m))).ToApiGatewayResponse();

        // Assert
        response.StatusCode.ShouldBe(200);
        response.Headers["Content-Type"].ShouldBe("application/json");
        JsonSerializer.Deserialize<OrderReceipt>(response.Body, WebOptions)
            .ShouldBe(new OrderReceipt("o-1", "Placed"));
    }

    [Theory]
    [InlineData("o-1", -1, 400, "validation.invalid_amount", "Bad Request")]
    [InlineData(PlaceOrderHandler.DuplicateOrderId, 1, 409, "order.already_exists", "Conflict")]
    public async Task ToApiGatewayResponse_ForAFailedCommand_MapsTheErrorCodeToAProblemDetailsResponse(
        string orderId, int amount, int expectedStatus, string expectedCode, string expectedTitle)
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

        // Act
        var response = (await encina.Send(new PlaceOrder(orderId, amount))).ToApiGatewayResponse();

        // Assert
        response.StatusCode.ShouldBe(expectedStatus);
        response.Headers["Content-Type"].ShouldBe("application/problem+json");
        using var body = JsonDocument.Parse(response.Body);
        body.RootElement.GetProperty("status").GetInt32().ShouldBe(expectedStatus);
        body.RootElement.GetProperty("title").GetString().ShouldBe(expectedTitle);
        body.RootElement.GetProperty("extensions").GetProperty("errorCode").GetString().ShouldBe(expectedCode);
    }

    [Fact]
    public async Task ToApiGatewayResponse_ForACommandWithoutAHandler_ReturnsNotFound()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

        // Act
        var response = (await encina.Send(new UnhandledCommand())).ToApiGatewayResponse();

        // Assert
        response.StatusCode.ShouldBe(404);
        response.Headers["Content-Type"].ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task ToApiGatewayResponse_WithAnExplicitErrorStatus_UsesIt()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

        // Act
        var response = (await encina.Send(new PlaceOrder("o-1", 0m))).ToApiGatewayResponse(errorStatusCode: 422);

        // Assert
        response.StatusCode.ShouldBe(422);
        JsonDocument.Parse(response.Body).RootElement.GetProperty("title").GetString().ShouldBe("An error occurred");
    }

    [Fact]
    public async Task ToCreatedResponse_ForASuccessfulCommand_ReturnsCreatedWithALocationHeader()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

        // Act
        var response = (await encina.Send(new PlaceOrder("o-7", 10m)))
            .ToCreatedResponse(receipt => $"/orders/{receipt.OrderId}");

        // Assert
        response.StatusCode.ShouldBe(201);
        response.Headers["Location"].ShouldBe("/orders/o-7");
    }

    [Fact]
    public async Task ToCreatedResponse_ForAFailedCommand_ReturnsTheProblemDetailsResponse()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

        // Act
        var response = (await encina.Send(new PlaceOrder("o-7", 0m)))
            .ToCreatedResponse(receipt => $"/orders/{receipt.OrderId}");

        // Assert
        response.StatusCode.ShouldBe(400);
        response.Headers.ContainsKey("Location").ShouldBeFalse();
    }

    [Fact]
    public async Task ToNoContentResponse_ForASuccessfulUnitCommand_ReturnsNoContent()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

        // Act
        var response = (await encina.Send(new CancelOrder("o-1"))).ToNoContentResponse();

        // Assert
        response.StatusCode.ShouldBe(204);
        response.Body.ShouldBeNull();
    }

    [Fact]
    public void ToNoContentResponse_ForAFailedResult_ReturnsTheProblemDetailsResponse()
    {
        // Arrange
        Either<EncinaError, Unit> failed = EncinaErrors.Create("order.not_found", "No such order.");

        // Act
        var response = failed.ToNoContentResponse();

        // Assert
        response.StatusCode.ShouldBe(404);
    }

    [Fact]
    public async Task ToHttpApiResponse_ForASuccessAndAFailure_ReturnsTheV2Responses()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

        // Act
        var ok = (await encina.Send(new PlaceOrder("o-1", 10m))).ToHttpApiResponse(successStatusCode: 202);
        var failed = (await encina.Send(new PlaceOrder("o-1", 0m))).ToHttpApiResponse();

        // Assert
        ok.StatusCode.ShouldBe(202);
        ok.Headers["Content-Type"].ShouldBe("application/json");
        failed.StatusCode.ShouldBe(400);
        failed.Headers["Content-Type"].ShouldBe("application/problem+json");
        JsonDocument.Parse(failed.Body).RootElement.GetProperty("extensions").GetProperty("errorCode").GetString()
            .ShouldBe("validation.invalid_amount");
    }

    [Theory]
    [InlineData("encina.authorization.unauthenticated", 401)]
    [InlineData("encina.authorization.denied", 403)]
    [InlineData("encina.guard.validation_failed", 400)]
    [InlineData("order.missing", 404)]
    [InlineData("order.duplicate", 409)]
    [InlineData("anything.else", 500)]
    public void ToProblemDetailsResponse_MapsEveryErrorFamilyToItsStatus(string code, int expectedStatus)
    {
        // Arrange
        var error = EncinaErrors.Create(code, "message");

        // Act
        var response = error.ToProblemDetailsResponse();

        // Assert
        response.StatusCode.ShouldBe(expectedStatus);
    }

    [Fact]
    public async Task ToApiGatewayResponse_SerializedByTheLambdaRuntime_KeepsTheStatusBodyAndHeaders()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var response = (await encina.Send(new PlaceOrder("o-1", 10m))).ToApiGatewayResponse();

        // Act
        var wire = Deserialize<APIGatewayProxyResponse>(Serialize(response));

        // Assert
        wire.StatusCode.ShouldBe(200);
        wire.Body.ShouldBe(response.Body);
        wire.Headers["Content-Type"].ShouldBe("application/json");
    }

    private sealed record UnhandledCommand : ICommand<Unit>;
}
