using System.Net;
using Encina.Messaging.Recoverability;
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

    private sealed class CustomTestException : Exception
    {
    }
}
