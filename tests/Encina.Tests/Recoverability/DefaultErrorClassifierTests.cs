using Encina.Messaging.Recoverability;
using Shouldly;

namespace Encina.Tests.Recoverability;

public sealed class DefaultErrorClassifierTests
{
    private readonly DefaultErrorClassifier _classifier = new();

    [Theory]
    [InlineData(typeof(TimeoutException), ErrorClassification.Transient)]
    [InlineData(typeof(TaskCanceledException), ErrorClassification.Transient)]
    [InlineData(typeof(IOException), ErrorClassification.Transient)]
    public void Classify_TransientExceptionTypes_ReturnsTransient(Type exceptionType, ErrorClassification expected)
    {
        // Arrange
        var exception = (Exception)Activator.CreateInstance(exceptionType, "Test error")!;
        var error = EncinaError.New(exception);

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(typeof(ArgumentException), ErrorClassification.Permanent)]
    [InlineData(typeof(ArgumentNullException), ErrorClassification.Permanent)]
    [InlineData(typeof(InvalidOperationException), ErrorClassification.Permanent)]
    [InlineData(typeof(NotSupportedException), ErrorClassification.Permanent)]
    [InlineData(typeof(UnauthorizedAccessException), ErrorClassification.Permanent)]
    [InlineData(typeof(FormatException), ErrorClassification.Permanent)]
    public void Classify_PermanentExceptionTypes_ReturnsPermanent(Type exceptionType, ErrorClassification expected)
    {
        // Arrange
        var exception = (Exception)Activator.CreateInstance(exceptionType, "Test error")!;
        var error = EncinaError.New(exception);

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void Classify_HttpRequestExceptionWith500_ReturnsTransient()
    {
        // Arrange
        var exception = new HttpRequestException("Server error", null, System.Net.HttpStatusCode.InternalServerError);
        var error = EncinaError.New(exception);

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(ErrorClassification.Transient);
    }

    [Fact]
    public void Classify_HttpRequestExceptionWith503_ReturnsTransient()
    {
        // Arrange
        var exception = new HttpRequestException("Service unavailable", null, System.Net.HttpStatusCode.ServiceUnavailable);
        var error = EncinaError.New(exception);

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(ErrorClassification.Transient);
    }

    [Fact]
    public void Classify_HttpRequestExceptionWith429_ReturnsTransient()
    {
        // Arrange
        var exception = new HttpRequestException("Too many requests", null, System.Net.HttpStatusCode.TooManyRequests);
        var error = EncinaError.New(exception);

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(ErrorClassification.Transient);
    }

    [Fact]
    public void Classify_HttpRequestExceptionWith400_ReturnsPermanent()
    {
        // Arrange
        var exception = new HttpRequestException("Bad request", null, System.Net.HttpStatusCode.BadRequest);
        var error = EncinaError.New(exception);

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(ErrorClassification.Permanent);
    }

    [Fact]
    public void Classify_HttpRequestExceptionWith404_ReturnsPermanent()
    {
        // Arrange
        var exception = new HttpRequestException("Not found", null, System.Net.HttpStatusCode.NotFound);
        var error = EncinaError.New(exception);

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(ErrorClassification.Permanent);
    }

    [Fact]
    public void Classify_HttpRequestExceptionWithoutStatusCode_ReturnsTransient()
    {
        // Arrange
        var exception = new HttpRequestException("Network error");
        var error = EncinaError.New(exception);

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert
        result.ShouldBe(ErrorClassification.Transient);
    }

    [Theory]
    [InlineData("validation failed", ErrorClassification.Permanent)]
    [InlineData("Resource not_found", ErrorClassification.Permanent)]
    [InlineData("unauthorized access", ErrorClassification.Permanent)]
    [InlineData("forbidden resource", ErrorClassification.Permanent)]
    [InlineData("invalid input", ErrorClassification.Permanent)]
    [InlineData("bad_request error", ErrorClassification.Permanent)]
    public void Classify_ErrorMessageWithPermanentPatterns_ReturnsPermanent(string message, ErrorClassification expected)
    {
        // Arrange
        var error = EncinaError.New(message);

        // Act
        var result = _classifier.Classify(error, null);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("timeout occurred", ErrorClassification.Transient)]
    [InlineData("service unavailable", ErrorClassification.Transient)]
    [InlineData("connection error", ErrorClassification.Transient)]
    [InlineData("network failure", ErrorClassification.Transient)]
    [InlineData("please retry", ErrorClassification.Transient)]
    [InlineData("rate_limit exceeded", ErrorClassification.Transient)]
    [InlineData("request throttle", ErrorClassification.Transient)]
    [InlineData("server busy", ErrorClassification.Transient)]
    [InlineData("system overload", ErrorClassification.Transient)]
    public void Classify_ErrorMessageWithTransientPatterns_ReturnsTransient(string message, ErrorClassification expected)
    {
        // Arrange
        var error = EncinaError.New(message);

        // Act
        var result = _classifier.Classify(error, null);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void Classify_UnknownErrorWithoutPatterns_ReturnsUnknown()
    {
        // Arrange
        var error = EncinaError.New("Some random error message");

        // Act
        var result = _classifier.Classify(error, null);

        // Assert
        result.ShouldBe(ErrorClassification.Unknown);
    }

    [Fact]
    public void Classify_ExceptionTakesPrecedenceOverMessage()
    {
        // Arrange - Permanent exception type with transient message pattern
        var exception = new ArgumentException("timeout occurred");
        var error = EncinaError.New(exception);

        // Act
        var result = _classifier.Classify(error, exception);

        // Assert - Exception type wins
        result.ShouldBe(ErrorClassification.Permanent);
    }

    [Fact]
    public void Classify_InnerExceptionIsChecked()
    {
        // Arrange - Use a custom exception that is not in the classifier's lists
        var innerException = new TimeoutException("Inner timeout");
        var outerException = new TestWrapperException("Outer error", innerException);
        var error = EncinaError.New(outerException);

        // Act
        var result = _classifier.Classify(error, outerException);

        // Assert - Should find transient TimeoutException in inner
        result.ShouldBe(ErrorClassification.Transient);
    }

    /// <summary>
    /// A wrapper exception that is not classified as transient or permanent.
    /// </summary>
    private sealed class TestWrapperException : SystemException
    {
        public TestWrapperException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [Fact]
    public void Classify_ErrorExceptionPropertyIsChecked()
    {
        // Arrange - Error with embedded exception
        var exception = new TimeoutException("Embedded timeout");
        var error = EncinaError.New(exception);

        // Act - Pass null as direct exception, classifier should check error.Exception
        var result = _classifier.Classify(error, null);

        // Assert
        result.ShouldBe(ErrorClassification.Transient);
    }
}
