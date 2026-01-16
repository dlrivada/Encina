using System.Net;
using BenchmarkDotNet.Attributes;

namespace Encina.AzureFunctions.Benchmarks;

/// <summary>
/// Benchmarks for error code to HTTP status code mapping performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class ErrorCodeMappingBenchmarks
{
    private readonly string[] _validationCodes =
    [
        "validation.email",
        "validation.required_field",
        "validation.invalid_format",
        "encina.guard.validation_failed"
    ];

    private readonly string[] _authorizationCodes =
    [
        "authorization.unauthenticated",
        "authorization.insufficient_roles",
        "authorization.policy_failed"
    ];

    private readonly string[] _notFoundCodes =
    [
        "user.not_found",
        "order.not_found",
        "encina.request.handler_missing",
        "resource.missing"
    ];

    private readonly string[] _conflictCodes =
    [
        "user.conflict",
        "email.already_exists",
        "username.duplicate"
    ];

    private readonly string[] _unknownCodes =
    [
        "internal.error",
        "database.connection_failed",
        "service.unavailable"
    ];

    [Benchmark(Baseline = true)]
    public HttpStatusCode MapValidationError()
    {
        return MapErrorCodeToStatusCode(_validationCodes[0]);
    }

    [Benchmark]
    public HttpStatusCode MapAuthorizationUnauthenticated()
    {
        return MapErrorCodeToStatusCode(_authorizationCodes[0]);
    }

    [Benchmark]
    public HttpStatusCode MapAuthorizationForbidden()
    {
        return MapErrorCodeToStatusCode(_authorizationCodes[1]);
    }

    [Benchmark]
    public HttpStatusCode MapNotFoundError()
    {
        return MapErrorCodeToStatusCode(_notFoundCodes[0]);
    }

    [Benchmark]
    public HttpStatusCode MapConflictError()
    {
        return MapErrorCodeToStatusCode(_conflictCodes[0]);
    }

    [Benchmark]
    public HttpStatusCode MapUnknownError()
    {
        return MapErrorCodeToStatusCode(_unknownCodes[0]);
    }

    [Benchmark]
    public HttpStatusCode MapMultipleValidationErrors()
    {
        var result = HttpStatusCode.OK;
        foreach (var code in _validationCodes)
        {
            result = MapErrorCodeToStatusCode(code);
        }
        return result;
    }

    [Benchmark]
    public HttpStatusCode MapMixedErrors()
    {
        var result = HttpStatusCode.OK;
        result = MapErrorCodeToStatusCode(_validationCodes[0]);
        result = MapErrorCodeToStatusCode(_authorizationCodes[0]);
        result = MapErrorCodeToStatusCode(_notFoundCodes[0]);
        result = MapErrorCodeToStatusCode(_conflictCodes[0]);
        result = MapErrorCodeToStatusCode(_unknownCodes[0]);
        return result;
    }

    // Mirrors the actual implementation
    private static HttpStatusCode MapErrorCodeToStatusCode(string code)
    {
        code = code.ToLowerInvariant();

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
}
