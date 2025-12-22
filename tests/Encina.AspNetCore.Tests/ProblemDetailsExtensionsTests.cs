using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Encina.AspNetCore.Tests;

public class ProblemDetailsExtensionsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task ToProblemDetails_ValidationError_Returns400()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "validation.invalid_input",
            message: "The input is invalid");

        // Act
        var (statusCode, problemDetails) = await ExecuteAndCaptureProblemDetails(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.BadRequest);
        problemDetails.Status.Should().Be(400);
        problemDetails.Title.Should().Be("Bad Request");
        problemDetails.Detail.Should().Be("The input is invalid");
    }

    [Fact]
    public async Task ToProblemDetails_UnauthenticatedError_Returns401()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "authorization.unauthenticated",
            message: "Authentication required");

        // Act
        var (statusCode, problemDetails) = await ExecuteAndCaptureProblemDetails(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.Unauthorized);
        problemDetails.Status.Should().Be(401);
        problemDetails.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task ToProblemDetails_AuthorizationError_Returns403()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "authorization.insufficient_roles",
            message: "Insufficient permissions");

        // Act
        var (statusCode, problemDetails) = await ExecuteAndCaptureProblemDetails(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.Forbidden);
        problemDetails.Status.Should().Be(403);
        problemDetails.Title.Should().Be("Forbidden");
    }

    [Fact]
    public async Task ToProblemDetails_NotFoundError_Returns404()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "user.not_found",
            message: "User not found");

        // Act
        var (statusCode, problemDetails) = await ExecuteAndCaptureProblemDetails(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.NotFound);
        problemDetails.Status.Should().Be(404);
        problemDetails.Title.Should().Be("Not Found");
    }

    [Fact]
    public async Task ToProblemDetails_HandlerMissingError_Returns404()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "encina.request.handler_missing",
            message: "No handler found");

        // Act
        var (statusCode, problemDetails) = await ExecuteAndCaptureProblemDetails(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.NotFound);
        problemDetails.Status.Should().Be(404);
    }

    [Fact]
    public async Task ToProblemDetails_ConflictError_Returns409()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "user.already_exists",
            message: "User already exists");

        // Act
        var (statusCode, problemDetails) = await ExecuteAndCaptureProblemDetails(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.Conflict);
        problemDetails.Status.Should().Be(409);
        problemDetails.Title.Should().Be("Conflict");
    }

    [Fact]
    public async Task ToProblemDetails_UnknownError_Returns500()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "unknown.error",
            message: "Something went wrong");

        // Act
        var (statusCode, problemDetails) = await ExecuteAndCaptureProblemDetails(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.InternalServerError);
        problemDetails.Status.Should().Be(500);
        problemDetails.Title.Should().Be("Internal Server Error");
    }

    [Fact]
    public async Task ToProblemDetails_IncludesTraceId()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "test.error",
            message: "Test error");

        // Act
        var (_, problemDetails) = await ExecuteAndCaptureProblemDetails(error);

        // Assert
        problemDetails.Extensions.Should().ContainKey("traceId");
        problemDetails.Extensions["traceId"].Should().NotBeNull();
    }

    [Fact]
    public async Task ToProblemDetails_IncludesCorrelationId_WhenHeaderPresent()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var error = EncinaErrors.Create(
            code: "test.error",
            message: "Test error");

        // Act
        var (_, problemDetails) = await ExecuteAndCaptureProblemDetails(
            error,
            configureRequest: request =>
            {
                request.Headers.Add("X-Correlation-ID", correlationId);
            });

        // Assert
        problemDetails.Extensions.Should().ContainKey("correlationId");
        problemDetails.Extensions["correlationId"]!.ToString().Should().Be(correlationId);
    }

    [Fact]
    public async Task ToProblemDetails_IncludesErrorCode_WhenAvailable()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "validation.invalid_email",
            message: "Invalid email format");

        // Act
        var (_, problemDetails) = await ExecuteAndCaptureProblemDetails(error);

        // Assert
        problemDetails.Extensions.Should().ContainKey("errorCode");
        problemDetails.Extensions["errorCode"]!.ToString().Should().Be("validation.invalid_email");
    }

    [Fact]
    public async Task ToProblemDetails_CustomStatusCode_OverridesDefault()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "user.not_found",
            message: "User not found");

        // Act
        var (statusCode, problemDetails) = await ExecuteAndCaptureProblemDetails(
            error,
            customStatusCode: 410); // Gone instead of Not Found

        // Assert
        statusCode.Should().Be(HttpStatusCode.Gone);
        problemDetails.Status.Should().Be(410);
    }

    [Fact]
    public async Task ToProblemDetails_IncludesRequestPath_WhenConfigured()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "test.error",
            message: "Test error");

        // Act
        var (_, problemDetails) = await ExecuteAndCaptureProblemDetails(
            error,
            configureServices: services =>
            {
                services.AddEncinaAspNetCore(options =>
                {
                    options.IncludeRequestPathInProblemDetails = true;
                });
            });

        // Assert
        problemDetails.Instance.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ToProblemDetails_ExcludesRequestPath_ByDefault()
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: "test.error",
            message: "Test error");

        // Act
        var (_, problemDetails) = await ExecuteAndCaptureProblemDetails(error);

        // Assert
        problemDetails.Instance.Should().BeNullOrEmpty();
    }

    [Theory]
    [InlineData("validation.email")]
    [InlineData("encina.guard.validation_failed")]
    public async Task ToProblemDetails_ValidationErrors_Return400(string errorCode)
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: errorCode,
            message: "Validation failed");

        // Act
        var (statusCode, _) = await ExecuteAndCaptureProblemDetails(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("resource.conflict")]
    [InlineData("email.already_exists")]
    [InlineData("username.duplicate")]
    public async Task ToProblemDetails_ConflictErrors_Return409(string errorCode)
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: errorCode,
            message: "Conflict occurred");

        // Act
        var (statusCode, _) = await ExecuteAndCaptureProblemDetails(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("user.not_found")]
    [InlineData("resource.missing")]
    [InlineData("encina.request.handler_missing")]
    public async Task ToProblemDetails_NotFoundErrors_Return404(string errorCode)
    {
        // Arrange
        var error = EncinaErrors.Create(
            code: errorCode,
            message: "Not found");

        // Act
        var (statusCode, _) = await ExecuteAndCaptureProblemDetails(error);

        // Assert
        statusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<(HttpStatusCode StatusCode, ProblemDetails ProblemDetails)> ExecuteAndCaptureProblemDetails(
        EncinaError error,
        int? customStatusCode = null,
        Action<IServiceCollection>? configureServices = null,
        Action<HttpRequestMessage>? configureRequest = null)
    {
        using var host = await CreateTestHost(
            ctx =>
            {
                var result = error.ToProblemDetails(ctx, customStatusCode);
                return result.ExecuteAsync(ctx);
            },
            configureServices);

        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        configureRequest?.Invoke(request);

        var response = await client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(json, JsonOptions);

        return (response.StatusCode, problemDetails!);
    }

    private static async Task<IHost> CreateTestHost(
        RequestDelegate endpoint,
        Action<IServiceCollection>? configureServices = null)
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddEncinaAspNetCore();
                    configureServices?.Invoke(services);
                });
                webHost.Configure(app =>
                {
                    app.Run(endpoint);
                });
            });

        var host = await hostBuilder.StartAsync();
        return host;
    }
}
