using Encina.AzureFunctions;
using Encina.AzureFunctions.Health;
using LanguageExt;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Encina.GuardTests.Cloud.AzureFunctions;

/// <summary>
/// Guard clause tests for the HTTP, context, middleware and health check APIs of
/// <c>Encina.AzureFunctions</c> not covered by <see cref="GuardClauseTests"/>.
/// </summary>
public class FunctionsGuardTests
{
    private static readonly Either<EncinaError, string> Success = "value";

    // ---------------------------------------------------------------- FunctionContextExtensions

    [Fact]
    public void GetCorrelationId_ThrowsOnNullContext()
    {
        FunctionContext context = null!;

        var action = () => context.GetCorrelationId();

        Should.Throw<ArgumentNullException>(action).ParamName.ShouldBe("context");
    }

    [Fact]
    public void GetCorrelationId_WithRequest_ThrowsOnNullContext()
    {
        FunctionContext context = null!;
        var request = CreateRequest();

        var action = () => context.GetCorrelationId(request);

        Should.Throw<ArgumentNullException>(action).ParamName.ShouldBe("context");
    }

    [Fact]
    public void GetCorrelationId_WithRequest_ThrowsOnNullRequest()
    {
        var context = Substitute.For<FunctionContext>();

        var action = () => context.GetCorrelationId(null!);

        Should.Throw<ArgumentNullException>(action).ParamName.ShouldBe("httpRequest");
    }

    [Fact]
    public void GetUserId_ThrowsOnNullContext()
    {
        FunctionContext context = null!;

        var action = () => context.GetUserId();

        Should.Throw<ArgumentNullException>(action).ParamName.ShouldBe("context");
    }

    [Fact]
    public void GetUserId_WithRequest_ThrowsOnNullContext()
    {
        FunctionContext context = null!;
        var request = CreateRequest();

        var action = () => context.GetUserId(request);

        Should.Throw<ArgumentNullException>(action).ParamName.ShouldBe("context");
    }

    [Fact]
    public void GetUserId_WithRequest_ThrowsOnNullRequest()
    {
        var context = Substitute.For<FunctionContext>();

        var action = () => context.GetUserId(null!);

        Should.Throw<ArgumentNullException>(action).ParamName.ShouldBe("httpRequest");
    }

    [Fact]
    public void GetInvocationId_ThrowsOnNullContext()
    {
        FunctionContext context = null!;

        var action = () => context.GetInvocationId();

        Should.Throw<ArgumentNullException>(action).ParamName.ShouldBe("context");
    }

    // ---------------------------------------------------------------- HttpResponseDataExtensions

    [Fact]
    public async Task ToHttpResponseData_ThrowsOnNullRequest()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await Success.ToHttpResponseData(null!));

        ex.ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task ToProblemDetailsResponse_ThrowsOnNullRequest()
    {
        EncinaError error = EncinaErrors.Create("test.code", "message");

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await global::Encina.AzureFunctions.HttpResponseDataExtensions.ToProblemDetailsResponse(error, null!));

        ex.ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task ToCreatedResponse_ThrowsOnNullRequest()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await Success.ToCreatedResponse(null!, v => v));

        ex.ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task ToCreatedResponse_ThrowsOnNullLocationFactory()
    {
        var request = CreateRequest();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await Success.ToCreatedResponse(request, null!));

        ex.ParamName.ShouldBe("locationFactory");
    }

    [Fact]
    public async Task ToNoContentResponse_ThrowsOnNullRequest()
    {
        Either<EncinaError, Unit> result = Unit.Default;

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await result.ToNoContentResponse(null!));

        ex.ParamName.ShouldBe("request");
    }

    // ---------------------------------------------------------------- Middleware

    [Fact]
    public async Task EncinaFunctionMiddleware_Invoke_ThrowsOnNullContext()
    {
        var middleware = CreateMiddleware();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => middleware.Invoke(null!, _ => Task.CompletedTask));

        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task EncinaFunctionMiddleware_Invoke_ThrowsOnNullNext()
    {
        var middleware = CreateMiddleware();
        var context = Substitute.For<FunctionContext>();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => middleware.Invoke(context, null!));

        ex.ParamName.ShouldBe("next");
    }

    // ---------------------------------------------------------------- AzureFunctionsHealthCheck

    [Fact]
    public void AzureFunctionsHealthCheck_Constructor_AcceptsOptions()
    {
        var check = new AzureFunctionsHealthCheck(Options.Create(new EncinaAzureFunctionsOptions()));

        check.Name.ShouldBe("encina-azure-functions");
    }

    private static EncinaFunctionMiddleware CreateMiddleware() =>
        new(
            Options.Create(new EncinaAzureFunctionsOptions()),
            NullLogger<EncinaFunctionMiddleware>.Instance);

    private static HttpRequestData CreateRequest() =>
        Substitute.For<HttpRequestData>(Substitute.For<FunctionContext>());
}
