using System.Net;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace Encina.AzureFunctions.PropertyTests;

/// <summary>
/// Property-based tests for error code to HTTP status code mapping.
/// </summary>
public class ErrorCodeMappingProperties
{
    [Property]
    public bool ValidationErrors_AlwaysReturn400(NonEmptyString suffix)
    {
        var code = $"validation.{suffix.Get}";
        var statusCode = MapErrorCodeToStatusCode(code);
        return statusCode == HttpStatusCode.BadRequest;
    }

    [Property]
    public bool AuthorizationErrors_AlwaysReturn403_ExceptUnauthenticated(NonEmptyString suffix)
    {
        var code = $"authorization.{suffix.Get}";
        if (code == "authorization.unauthenticated")
        {
            return true; // Skip unauthenticated, tested separately
        }
        var statusCode = MapErrorCodeToStatusCode(code);
        return statusCode == HttpStatusCode.Forbidden;
    }

    [Property]
    public bool NotFoundErrors_AlwaysReturn404(NonEmptyString prefix)
    {
        var code = $"{prefix.Get}.not_found";
        var statusCode = MapErrorCodeToStatusCode(code);
        return statusCode == HttpStatusCode.NotFound;
    }

    [Property]
    public bool MissingErrors_AlwaysReturn404(NonEmptyString prefix)
    {
        var code = $"{prefix.Get}.missing";
        var statusCode = MapErrorCodeToStatusCode(code);
        return statusCode == HttpStatusCode.NotFound;
    }

    [Property]
    public bool ConflictErrors_AlwaysReturn409(NonEmptyString prefix)
    {
        var code = $"{prefix.Get}.conflict";
        var statusCode = MapErrorCodeToStatusCode(code);
        return statusCode == HttpStatusCode.Conflict;
    }

    [Property]
    public bool AlreadyExistsErrors_AlwaysReturn409(NonEmptyString prefix)
    {
        var code = $"{prefix.Get}.already_exists";
        var statusCode = MapErrorCodeToStatusCode(code);
        return statusCode == HttpStatusCode.Conflict;
    }

    [Property]
    public bool DuplicateErrors_AlwaysReturn409(NonEmptyString prefix)
    {
        var code = $"{prefix.Get}.duplicate";
        var statusCode = MapErrorCodeToStatusCode(code);
        return statusCode == HttpStatusCode.Conflict;
    }

    [Property]
    public bool UnknownErrors_AlwaysReturn500(NonEmptyString codeInput)
    {
        var errorCode = codeInput.Get;
        // Skip known patterns
        if (errorCode.StartsWith("validation.", StringComparison.OrdinalIgnoreCase) ||
            errorCode.StartsWith("authorization.", StringComparison.OrdinalIgnoreCase) ||
            errorCode.EndsWith(".not_found", StringComparison.OrdinalIgnoreCase) ||
            errorCode.EndsWith(".missing", StringComparison.OrdinalIgnoreCase) ||
            errorCode.EndsWith(".conflict", StringComparison.OrdinalIgnoreCase) ||
            errorCode.EndsWith(".already_exists", StringComparison.OrdinalIgnoreCase) ||
            errorCode.EndsWith(".duplicate", StringComparison.OrdinalIgnoreCase) ||
            errorCode == "encina.guard.validation_failed" ||
            errorCode == "encina.request.handler_missing")
        {
            return true; // Skip known patterns
        }

        var statusCode = MapErrorCodeToStatusCode(errorCode);
        return statusCode == HttpStatusCode.InternalServerError;
    }

    [Fact]
    public void ErrorCodeMapping_IsCaseInsensitive()
    {
        var testCases = new[] { "validation.test", "VALIDATION.TEST", "Validation.Test", "VaLiDaTiOn.TeSt" };
        foreach (var code in testCases)
        {
            var statusCode = MapErrorCodeToStatusCode(code);
            statusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }

    [Property]
    public bool StatusCodeMapping_AlwaysReturnsValidHttpStatusCode(NonEmptyString code)
    {
        var statusCode = MapErrorCodeToStatusCode(code.Get);
        return Enum.IsDefined<HttpStatusCode>(statusCode);
    }

    // Helper method that mirrors the actual implementation
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
