using System.Text.Json;
using Encina.AzureFunctions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static Encina.IntegrationTests.Cloud.AzureFunctions.AzureFunctionsTestSupport;

namespace Encina.IntegrationTests.Cloud.AzureFunctions;

/// <summary>
/// Converts the results of the real Encina pipeline with <see cref="HttpResponseDataExtensions"/> and reads what reaches the response.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Cloud", "AzureFunctions")]
public sealed class HttpResponseDataIntegrationTests
{
    private sealed record UnhandledCommand : ICommand<Unit>;

    private static TestHttpRequestData NewRequest(IServiceProvider services, string invocationId = "inv-1") =>
        new(new TestFunctionContext(services, "PlaceOrder", invocationId));

    [Fact]
    public async Task ToHttpResponseData_ForASuccessfulCommand_WritesTheReceiptAsJson()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var request = NewRequest(scope.ServiceProvider);

        // Act
        var response = (TestHttpResponseData)await (await encina.Send(new PlaceOrder("o-1", 10m))).ToHttpResponseData(request);

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        response.Headers.GetValues("Content-Type").ShouldBe(["application/json"]);
        using var body = JsonDocument.Parse(response.ReadBody());
        body.RootElement.GetProperty("orderId").GetString().ShouldBe("o-1");
        body.RootElement.GetProperty("status").GetString().ShouldBe("Placed");
    }

    [Theory]
    [InlineData("o-1", -1, 400, "validation.invalid_amount")]
    [InlineData(PlaceOrderHandler.DuplicateOrderId, 1, 409, "order.already_exists")]
    public async Task ToHttpResponseData_ForAFailedCommand_WritesAProblemDetailsBodyWithTheTraceAndCorrelation(
        string orderId, int amount, int expectedStatus, string expectedCode)
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var request = NewRequest(scope.ServiceProvider, "inv-77");
        request.FunctionContext.Items[EncinaCorrelationKey] = "corr-77";

        // Act
        var response = (TestHttpResponseData)await (await encina.Send(new PlaceOrder(orderId, amount))).ToHttpResponseData(request);

        // Assert
        ((int)response.StatusCode).ShouldBe(expectedStatus);
        response.Headers.GetValues("Content-Type").ShouldBe(["application/problem+json"]);
        using var body = JsonDocument.Parse(response.ReadBody());
        body.RootElement.GetProperty("status").GetInt32().ShouldBe(expectedStatus);
        body.RootElement.GetProperty("instance").GetString().ShouldBe("/api/orders?x=1");
        var extensions = body.RootElement.GetProperty("extensions");
        extensions.GetProperty("errorCode").GetString().ShouldBe(expectedCode);
        extensions.GetProperty("traceId").GetString().ShouldBe("inv-77");
        extensions.GetProperty("correlationId").GetString().ShouldBe("corr-77");
    }

    [Fact]
    public async Task ToHttpResponseData_ForACommandWithoutAHandler_ReturnsNotFound()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var request = NewRequest(scope.ServiceProvider);

        // Act
        var response = await (await encina.Send(new UnhandledCommand())).ToHttpResponseData(request);

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
        using var body = JsonDocument.Parse(((TestHttpResponseData)response).ReadBody());
        body.RootElement.GetProperty("extensions").GetProperty("errorCode").GetString()
            .ShouldBe(global::Encina.EncinaErrorCodes.RequestHandlerMissing);
    }

    [Fact]
    public async Task ToHttpResponseData_WithExplicitStatusCodes_UsesThem()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var request = NewRequest(scope.ServiceProvider);

        // Act
        var accepted = await (await encina.Send(new PlaceOrder("o-1", 10m))).ToHttpResponseData(request, successStatusCode: 202);
        var rejected = await (await encina.Send(new PlaceOrder("o-1", 0m))).ToHttpResponseData(request, errorStatusCode: 422);

        // Assert
        ((int)accepted.StatusCode).ShouldBe(202);
        ((int)rejected.StatusCode).ShouldBe(422);
    }

    [Fact]
    public async Task ToCreatedResponse_ForASuccessfulCommand_ReturnsCreatedWithALocationHeader()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var request = NewRequest(scope.ServiceProvider);

        // Act
        var response = await (await encina.Send(new PlaceOrder("o-7", 10m)))
            .ToCreatedResponse(request, receipt => $"/orders/{receipt.OrderId}");

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Created);
        response.Headers.GetValues("Location").ShouldBe(["/orders/o-7"]);
    }

    [Fact]
    public async Task ToCreatedResponse_ForAFailedCommand_ReturnsTheProblemDetailsResponse()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var request = NewRequest(scope.ServiceProvider);

        // Act
        var response = await (await encina.Send(new PlaceOrder("o-7", 0m)))
            .ToCreatedResponse(request, receipt => $"/orders/{receipt.OrderId}");

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
        response.Headers.TryGetValues("Location", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task ToNoContentResponse_ForASuccessAndAFailure_ReturnsNoContentOrTheProblemDetails()
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        var request = NewRequest(scope.ServiceProvider);
        Either<EncinaError, Unit> failed = EncinaErrors.Create("order.not_found", "No such order.");

        // Act
        var ok = await (await encina.Send(new CancelOrder("o-1"))).ToNoContentResponse(request);
        var notFound = await failed.ToNoContentResponse(request);

        // Assert
        ok.StatusCode.ShouldBe(System.Net.HttpStatusCode.NoContent);
        notFound.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("encina.authorization.unauthenticated", 401)]
    [InlineData("encina.authorization.denied", 403)]
    [InlineData("encina.guard.validation_failed", 400)]
    [InlineData("order.missing", 404)]
    [InlineData("order.duplicate", 409)]
    [InlineData("anything.else", 500)]
    public async Task ToProblemDetailsResponse_MapsEveryErrorFamilyToItsStatus(string code, int expectedStatus)
    {
        // Arrange
        await using var provider = CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var request = NewRequest(scope.ServiceProvider);
        var error = EncinaErrors.Create(code, "message");

        // Act
        var response = await error.ToProblemDetailsResponse(request);

        // Assert
        ((int)response.StatusCode).ShouldBe(expectedStatus);
    }

    private const string EncinaCorrelationKey = "Encina.CorrelationId";
}
