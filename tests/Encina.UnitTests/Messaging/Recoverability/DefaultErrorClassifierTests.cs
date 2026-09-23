using System.Net;
using Encina.Messaging.Choreography;
using Encina.Messaging.Recoverability;
using Encina.Messaging.RoutingSlip;
using Encina.Messaging.Sagas;
using LanguageExt;
using Shouldly;

namespace Encina.UnitTests.Messaging.Recoverability;

/// <summary>
/// Unit tests for <see cref="DefaultErrorClassifier"/>.
/// </summary>
public sealed class DefaultErrorClassifierTests
{
    private readonly DefaultErrorClassifier _classifier = new();

    #region Exception-based classification

    [Fact]
    public void Classify_TimeoutException_ReturnsTransient()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");
        var exception = new TimeoutException();

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(ErrorClassification.Transient);
    }

    [Fact]
    public void Classify_TaskCanceledException_ReturnsTransient()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");
        var exception = new TaskCanceledException();

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(ErrorClassification.Transient);
    }

    [Fact]
    public void Classify_IOException_ReturnsTransient()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");
        var exception = new IOException();

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(ErrorClassification.Transient);
    }

    [Fact]
    public void Classify_HttpRequestException_5xx_ReturnsTransient()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");
        var exception = new HttpRequestException(null, null, HttpStatusCode.InternalServerError);

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(ErrorClassification.Transient);
    }

    [Fact]
    public void Classify_HttpRequestException_503_ReturnsTransient()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");
        var exception = new HttpRequestException(null, null, HttpStatusCode.ServiceUnavailable);

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(ErrorClassification.Transient);
    }

    [Fact]
    public void Classify_HttpRequestException_429_ReturnsTransient()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");
        var exception = new HttpRequestException(null, null, HttpStatusCode.TooManyRequests);

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(ErrorClassification.Transient);
    }

    [Fact]
    public void Classify_HttpRequestException_4xx_ReturnsPermanent()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");
        var exception = new HttpRequestException(null, null, HttpStatusCode.BadRequest);

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(ErrorClassification.Permanent);
    }

    [Fact]
    public void Classify_HttpRequestException_NotFound_ReturnsPermanent()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");
        var exception = new HttpRequestException(null, null, HttpStatusCode.NotFound);

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(ErrorClassification.Permanent);
    }

    [Fact]
    public void Classify_HttpRequestException_NoStatusCode_ReturnsTransient()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");
        var exception = new HttpRequestException("Network error");

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(ErrorClassification.Transient);
    }

    [Fact]
    public void Classify_ArgumentException_ReturnsPermanent()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");
        var exception = new ArgumentException("Invalid argument");

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(ErrorClassification.Permanent);
    }

    [Fact]
    public void Classify_ArgumentNullException_ReturnsPermanent()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");
        var exception = new ArgumentNullException("param");

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(ErrorClassification.Permanent);
    }

    [Fact]
    public void Classify_InvalidOperationException_ReturnsPermanent()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");
        var exception = new InvalidOperationException();

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(ErrorClassification.Permanent);
    }

    [Fact]
    public void Classify_NotSupportedException_ReturnsPermanent()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");
        var exception = new NotSupportedException();

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(ErrorClassification.Permanent);
    }

    [Fact]
    public void Classify_UnauthorizedAccessException_ReturnsPermanent()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");
        var exception = new UnauthorizedAccessException();

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(ErrorClassification.Permanent);
    }

    [Fact]
    public void Classify_FormatException_ReturnsPermanent()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");
        var exception = new FormatException();

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(ErrorClassification.Permanent);
    }

    [Fact]
    public void Classify_ExceptionWithTransientInnerException_ReturnsTransient()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");
        var innerException = new TimeoutException();
        var exception = new AggregateException("Wrapper", innerException);

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(ErrorClassification.Transient);
    }

    [Fact]
    public void Classify_UnknownException_ReturnsUnknown()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");
        var exception = new CustomTestException();

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(ErrorClassification.Unknown);
    }

    #endregion

    #region Error message-based classification

    [Theory]
    [InlineData("validation failed")]
    [InlineData("VALIDATION error")]
    [InlineData("not_found")]
    [InlineData("Resource NOT_FOUND")]
    [InlineData("unauthorized access")]
    [InlineData("UNAUTHORIZED")]
    [InlineData("forbidden action")]
    [InlineData("FORBIDDEN")]
    [InlineData("invalid request")]
    [InlineData("INVALID input")]
    [InlineData("bad_request")]
    [InlineData("BAD_REQUEST error")]
    public void Classify_PermanentErrorPatterns_ReturnsPermanent(string message)
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", message);

        // Act
        var result = _classifier.Classify(error, null);

        // Assert
        result.ShouldBe(ErrorClassification.Permanent);
    }

    [Theory]
    [InlineData("timeout occurred")]
    [InlineData("TIMEOUT")]
    [InlineData("service unavailable")]
    [InlineData("UNAVAILABLE")]
    [InlineData("connection failed")]
    [InlineData("CONNECTION error")]
    [InlineData("network error")]
    [InlineData("NETWORK failure")]
    [InlineData("retry later")]
    [InlineData("RETRY")]
    [InlineData("rate_limit exceeded")]
    [InlineData("RATE_LIMIT")]
    [InlineData("throttle")]
    [InlineData("THROTTLE")]
    [InlineData("server busy")]
    [InlineData("BUSY")]
    [InlineData("overload")]
    [InlineData("OVERLOAD")]
    public void Classify_TransientErrorPatterns_ReturnsTransient(string message)
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", message);

        // Act
        var result = _classifier.Classify(error, null);

        // Assert
        result.ShouldBe(ErrorClassification.Transient);
    }

    [Fact]
    public void Classify_EmptyMessage_ReturnsUnknown()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", string.Empty);

        // Act
        var result = _classifier.Classify(error, null);

        // Assert
        result.ShouldBe(ErrorClassification.Unknown);
    }

    [Fact]
    public void Classify_NoPatternMatch_ReturnsUnknown()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Some generic error");

        // Act
        var result = _classifier.Classify(error, null);

        // Assert
        result.ShouldBe(ErrorClassification.Unknown);
    }

    #endregion

    #region Error with Exception in EncinaError

    [Fact]
    public void Classify_ErrorWithException_ClassifiesException()
    {
        // Arrange
        var innerException = new TimeoutException();
        var error = EncinaError.New(innerException, "[test.error] Test error");

        // Act
        var result = _classifier.Classify(error, null);

        // Assert
        result.ShouldBe(ErrorClassification.Transient);
    }

    #endregion

    #region Error-code classification

    [Theory]
    [InlineData(EncinaErrorCodes.HandlerMissing)]
    [InlineData(EncinaErrorCodes.RequestHandlerMissing)]
    [InlineData(EncinaErrorCodes.RequestHandlerTypeMismatch)]
    [InlineData(EncinaErrorCodes.NotificationMissingHandle)]
    [InlineData(EncinaErrorCodes.AuthorizationUnauthorized)]
    [InlineData(EncinaErrorCodes.AuthorizationForbidden)]
    [InlineData(EncinaErrorCodes.AuthorizationPolicyFailed)]
    [InlineData(EncinaErrorCodes.AuthorizationResourceDenied)]
    [InlineData("Encina.guard.validation_failed")]
    [InlineData("Repository.ValidationFailed")]
    [InlineData("processor.validation_failed")]
    [InlineData("gdpr.compliance_validation_failed")]
    [InlineData("aiact.compliance_validation_failed")]
    [InlineData("consent.missing")]
    [InlineData("consent.expired")]
    [InlineData("consent.withdrawn")]
    [InlineData("consent.requires_reconsent")]
    [InlineData("consent.version_mismatch")]
    [InlineData("dsr.restriction_active")]
    [InlineData("dsr.subject_id_missing")]
    [InlineData("dsr.identity_not_verified")]
    public void Classify_ExplicitPermanentErrorCode_ReturnsPermanent(string code)
    {
        // The message deliberately contains a transient word: the code decides first.
        var error = EncinaErrors.Create(code, "Please retry later");

        _classifier.Classify(error, null).ShouldBe(ErrorClassification.Permanent);
    }

    [Theory]
    [InlineData(EncinaErrorCodes.Timeout)]
    [InlineData(EncinaErrorCodes.RateLimitExceeded)]
    public void Classify_ExplicitTransientErrorCode_ReturnsTransient(string code)
    {
        // The message deliberately contains a permanent word: the code decides first.
        var error = EncinaErrors.Create(code, "Invalid state, try again");

        _classifier.Classify(error, null).ShouldBe(ErrorClassification.Transient);
    }

    [Theory]
    [InlineData("marten.aggregate_not_found")]
    [InlineData(SagaErrorCodes.NotFound)]
    [InlineData(ChoreographyErrorCodes.SagaNotFound)]
    [InlineData(RoutingSlipErrorCodes.NotFound)]
    [InlineData(SagaErrorCodes.InvalidStatus)]
    [InlineData(ChoreographyErrorCodes.InvalidState)]
    public void Classify_NotFoundOrInvalidCodeOutsideTheExplicitList_IsNotPermanentByCode(string code)
    {
        // An out-of-order event or a saga state race can succeed on a later attempt: these codes are not
        // matched by substring ("not_found", "invalid"), so the message decides as it did before codes
        // were considered — here a neutral message, so the error is Unknown (retried).
        var error = EncinaErrors.Create(code, "The target could not be loaded yet");

        _classifier.Classify(error, null).ShouldBe(ErrorClassification.Unknown);
    }

    [Theory]
    [InlineData(SagaErrorCodes.NotFound)]
    [InlineData(SagaErrorCodes.InvalidStatus)]
    public void Classify_CodeOutsideTheExplicitList_FallsBackToTheMessage(string code)
    {
        var error = EncinaErrors.Create(code, "Store unavailable, retry later");

        _classifier.Classify(error, null).ShouldBe(ErrorClassification.Transient);
    }

    [Theory]
    [InlineData("consent.event_history_unavailable")]
    [InlineData("encina.handler.missing_extra")]
    [InlineData("custom.consent.missing")]
    public void Classify_CodeIsMatchedExactlyNotBySubstring(string code)
    {
        var error = EncinaErrors.Create(code, "Something happened");

        _classifier.Classify(error, null).ShouldBe(ErrorClassification.Unknown);
    }

    [Fact]
    public void Classify_ExceptionTakesPrecedenceOverCode()
    {
        var error = EncinaErrors.Create("consent.missing", "Consent missing");

        _classifier.Classify(error, new TimeoutException()).ShouldBe(ErrorClassification.Transient);
    }

    #endregion

    #region Errors with a causing exception

    [Fact]
    public void Classify_DispatcherErrorForUnknownException_DoesNotMatchHandlerNameInMessage()
    {
        // The dispatcher builds "Unexpected exception in notification handler CacheInvalidationHandler ..."
        // around the thrown exception: "invalid" in the handler name must not make it permanent.
        var error = EncinaErrors.FromException(
            EncinaErrorCodes.NotificationException,
            new DownstreamUnavailableException(),
            "Unexpected exception in notification handler CacheInvalidationHandler for OrderPlaced. " +
            "Handlers should return Left for expected failures instead of throwing exceptions.");

        _classifier.Classify(error, null).ShouldNotBe(ErrorClassification.Permanent);
        _classifier.Classify(error, error.GetCause().MatchUnsafe(ex => ex, () => null))
            .ShouldNotBe(ErrorClassification.Permanent);
    }

    [Fact]
    public void Classify_DispatcherErrorForTransientException_ReturnsTransient()
    {
        var error = EncinaErrors.FromException(
            EncinaErrorCodes.NotificationException,
            new TimeoutException(),
            "Unexpected exception in notification handler CacheInvalidationHandler for OrderPlaced.");

        _classifier.Classify(error, error.GetCause().MatchUnsafe(ex => ex, () => null))
            .ShouldBe(ErrorClassification.Transient);
    }

    [Fact]
    public void Classify_UnknownExceptionPassedIn_DoesNotApplyMessagePatterns()
    {
        var error = EncinaErrors.Create("test.error", "Invalid handler CacheInvalidationHandler");

        _classifier.Classify(error, new DownstreamUnavailableException()).ShouldBe(ErrorClassification.Unknown);
    }

    [Fact]
    public void Classify_ErrorWithoutCause_WhenItsCarrierIsPassedBack_StillAppliesMessagePatterns()
    {
        // The recoverability pipeline passes EncinaError.Exception, which for an error without a cause is
        // the internal carrier of code and message: the message is still free text and is matched.
        var error = EncinaErrors.Create("test.error", "invalid request");
        var carrier = error.Exception.MatchUnsafe(ex => ex, () => null);

        _classifier.Classify(error, carrier).ShouldBe(ErrorClassification.Permanent);
    }

    #endregion

    private sealed class DownstreamUnavailableException : Exception
    {
    }

    private sealed class CustomTestException : Exception
    {
    }
}
