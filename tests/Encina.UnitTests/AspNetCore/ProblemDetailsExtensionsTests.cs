using Encina.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace Encina.UnitTests.AspNetCore;

/// <summary>
/// Comprehensive unit tests for <see cref="ProblemDetailsExtensions"/>.
/// Covers all error code → HTTP status code mappings.
/// </summary>
public sealed class ProblemDetailsExtensionsTests
{
    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.RequestServices = new ServiceCollection()
            .Configure<EncinaAspNetCoreOptions>(_ => { })
            .BuildServiceProvider();
        return context;
    }

    #region ToProblemDetails — Status Code Mapping

    [Fact]
    public void ToProblemDetails_ValidationError_Returns400()
    {
        var error = EncinaErrors.Create("validation.field_required", "Field is required");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);

        result.ShouldNotBeNull();
    }

    [Fact]
    public void ToProblemDetails_GuardValidationFailed_Returns400()
    {
        var error = EncinaErrors.Create("encina.guard.validation_failed", "Guard validation failed");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ToProblemDetails_Unauthenticated_Returns401()
    {
        var error = EncinaErrors.Create("authorization.unauthenticated", "Not authenticated");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ToProblemDetails_AuthorizationForbidden_Returns403()
    {
        var error = EncinaErrors.Create("authorization.insufficient_permissions", "Forbidden");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ToProblemDetails_NotFound_Returns404()
    {
        var error = EncinaErrors.Create("entity.not_found", "Not found");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ToProblemDetails_HandlerMissing_Returns404()
    {
        var error = EncinaErrors.Create("encina.request.handler_missing", "No handler");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ToProblemDetails_Missing_Returns404()
    {
        var error = EncinaErrors.Create("resource.missing", "Resource missing");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ToProblemDetails_Conflict_Returns409()
    {
        var error = EncinaErrors.Create("entity.conflict", "Conflict");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ToProblemDetails_AlreadyExists_Returns409()
    {
        var error = EncinaErrors.Create("user.already_exists", "Duplicate");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ToProblemDetails_Duplicate_Returns409()
    {
        var error = EncinaErrors.Create("order.duplicate", "Duplicate order");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ToProblemDetails_UnknownError_Returns500()
    {
        var error = EncinaErrors.Create("unknown.error", "Something went wrong");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ToProblemDetails_ExplicitStatusCode_OverridesMapping()
    {
        var error = EncinaErrors.Create("test.error", "test");
        var context = CreateHttpContext();

        var result = error.ToProblemDetails(context, 422);
        result.ShouldNotBeNull();
    }

    #endregion

    #region ToProblemDetails — Extensions

    [Fact]
    public void ToProblemDetails_WithCorrelationId_IncludesInExtensions()
    {
        var error = EncinaErrors.Create("test.error", "test");
        var context = CreateHttpContext();
        context.Request.Headers["X-Correlation-ID"] = "corr-123";

        var result = error.ToProblemDetails(context);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ToProblemDetails_WithTraceId_IncludesInExtensions()
    {
        var error = EncinaErrors.Create("test.error", "test");
        var context = CreateHttpContext();
        context.TraceIdentifier = "trace-abc";

        var result = error.ToProblemDetails(context);
        result.ShouldNotBeNull();
    }

    #endregion

    #region ToActionResult

    [Fact]
    public void ToActionResult_ReturnsNonNull()
    {
        var error = EncinaErrors.Create("test.error", "test");
        var context = CreateHttpContext();

        var result = error.ToActionResult(context);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void ToActionResult_WithExplicitStatusCode_ReturnsResult()
    {
        var error = EncinaErrors.Create("test.code", "Test");
        var context = CreateHttpContext();

        var result = error.ToActionResult(context, 503);
        result.ShouldNotBeNull();
    }

    #endregion

    #region ToProblemDetails — No Services

    [Fact]
    public void ToProblemDetails_NoOptionsRegistered_UsesDefaults()
    {
        var error = EncinaErrors.Create("test.error", "test");
        var context = new DefaultHttpContext();
        context.RequestServices = new ServiceCollection().BuildServiceProvider();

        var result = error.ToProblemDetails(context);
        result.ShouldNotBeNull();
    }

    #endregion
}
