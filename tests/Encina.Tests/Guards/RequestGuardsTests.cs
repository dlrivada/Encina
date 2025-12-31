using Shouldly;
using LanguageExt;

namespace Encina.Tests.Guards;

/// <summary>
/// Guard clause tests for Request validation in <see cref="EncinaRequestGuards"/>.
/// Tests ensure proper null validation and error handling for requests.
/// </summary>
public sealed class RequestGuardsTests
{
    #region Helper Methods

    private static EncinaError ExtractLeftError<TRight>(Either<EncinaError, TRight> error, string message = "Expected Left but got Right")
        => error.Match(
            Left: e => e,
            Right: _ => throw new InvalidOperationException(message));

    #endregion

    #region TryValidateRequest Tests

    [Fact]
    public void TryValidateRequest_WithValidRequest_ShouldReturnTrue()
    {
        // Arrange
        var request = new TestRequest("test");

        // Act
        var result = EncinaRequestGuards.TryValidateRequest<string>(request, out var error);

        // Assert
        result.ShouldBeTrue();
        error.ShouldBeBottom(); // Default Either is neither Left nor Right
    }

    [Fact]
    public void TryValidateRequest_WithNullRequest_ShouldReturnFalse()
    {
        // Arrange
        IRequest<string>? request = null;

        // Act
        var result = EncinaRequestGuards.TryValidateRequest<string>(request, out var error);

        // Assert
        result.ShouldBeFalse();
        error.ShouldBeError();

        var encinaError = ExtractLeftError(error);

        encinaError.GetEncinaCode().ShouldBe(EncinaErrorCodes.RequestNull);
        encinaError.Message.ShouldContain("request");
        encinaError.Message.ShouldContain("cannot be null");
    }

    [Fact]
    public void TryValidateRequest_WithDifferentResponseTypes_ShouldHandleCorrectly()
    {
        // Arrange
        IRequest<int>? nullIntRequest = null;
        IRequest<Guid>? nullGuidRequest = null;
        var validRequest = new TestRequest("value");

        // Act
        var result1 = EncinaRequestGuards.TryValidateRequest<int>(nullIntRequest, out var error1);
        var result2 = EncinaRequestGuards.TryValidateRequest<Guid>(nullGuidRequest, out var error2);
        var result3 = EncinaRequestGuards.TryValidateRequest<string>(validRequest, out _);

        // Assert
        result1.ShouldBeFalse();
        result2.ShouldBeFalse();
        result3.ShouldBeTrue();

        error1.ShouldBeError();
        error2.ShouldBeError();
    }

    #endregion

    #region TryValidateHandler Tests

    [Fact]
    public void TryValidateHandler_WithValidHandler_ShouldReturnTrue()
    {
        // Arrange
        var handler = new TestRequestHandler();

        // Act
        var result = EncinaRequestGuards.TryValidateHandler<string>(
            handler,
            typeof(TestRequest),
            typeof(string),
            out var error);

        // Assert
        result.ShouldBeTrue();
        error.ShouldBeBottom();
    }

    [Fact]
    public void TryValidateHandler_WithNullHandler_ShouldReturnFalse()
    {
        // Arrange
        object? handler = null;

        // Act
        var result = EncinaRequestGuards.TryValidateHandler<string>(
            handler,
            typeof(TestRequest),
            typeof(string),
            out var error);

        // Assert
        result.ShouldBeFalse();
        error.ShouldBeError();

        var encinaError = ExtractLeftError(error);

        encinaError.GetEncinaCode().ShouldBe(EncinaErrorCodes.RequestHandlerMissing);
        encinaError.Message.ShouldContain("TestRequest");
        encinaError.Message.ShouldContain("String");
    }

    [Fact]
    public void TryValidateHandler_WithNullHandler_ShouldIncludeMetadata()
    {
        // Arrange
        object? handler = null;

        // Act
        EncinaRequestGuards.TryValidateHandler<string>(
            handler,
            typeof(TestRequest),
            typeof(string),
            out var error);

        // Assert
        var encinaError = ExtractLeftError(error, "Expected Left");

        var metadata = encinaError.GetMetadata();
        metadata.ShouldNotBeNull();
        metadata.ShouldContainKey("requestType");
        metadata.ShouldContainKey("responseType");
        metadata.ShouldContainKey("stage");
        metadata["stage"].ShouldBe("handler_resolution");
    }

    #endregion

    #region TryValidateHandlerType Tests

    [Fact]
    public void TryValidateHandlerType_WithCorrectType_ShouldReturnTrue()
    {
        // Arrange
        var handler = new TestRequestHandler();
        var expectedType = typeof(IRequestHandler<TestRequest, string>);

        // Act
        var result = EncinaRequestGuards.TryValidateHandlerType<string>(
            handler,
            expectedType,
            typeof(TestRequest),
            out var error);

        // Assert
        result.ShouldBeTrue();
        error.ShouldBeBottom();
    }

    [Fact]
    public void TryValidateHandlerType_WithWrongType_ShouldReturnFalse()
    {
        // Arrange
        var handler = new object();
        var expectedType = typeof(IRequestHandler<TestRequest, string>);

        // Act
        var result = EncinaRequestGuards.TryValidateHandlerType<string>(
            handler,
            expectedType,
            typeof(TestRequest),
            out var error);

        // Assert
        result.ShouldBeFalse();
        error.ShouldBeError();

        var encinaError = ExtractLeftError(error, "Expected Left");

        encinaError.GetEncinaCode().ShouldBe(EncinaErrorCodes.RequestHandlerTypeMismatch);
        encinaError.Message.ShouldContain("does not implement");
    }

    #endregion

    #region Test Types

    private sealed record TestRequest(string Value) : IRequest<string>;

    private sealed class TestRequestHandler : IRequestHandler<TestRequest, string>
    {
        public Task<Either<EncinaError, string>> Handle(TestRequest request, CancellationToken cancellationToken)
            => Task.FromResult(Either<EncinaError, string>.Right(request.Value.ToUpperInvariant()));
    }

    #endregion
}
