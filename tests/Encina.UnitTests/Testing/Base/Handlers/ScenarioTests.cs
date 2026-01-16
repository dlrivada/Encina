using Encina.Testing.Handlers;
using Encina.UnitTests.Testing.Base.TestFixtures;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Testing.Base.Handlers;

public sealed class ScenarioTests
{
    #region Test Infrastructure

    #endregion

    #region Describe Tests

    [Fact]
    public void Describe_CreatesScenarioWithDescription()
    {
        // Act
        var scenario = Scenario<TestRequest, TestResponse>.Describe("Test scenario");

        // Assert
        scenario.Description.ShouldBe("Test scenario");
    }

    [Fact]
    public void Describe_NullDescription_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            Scenario<TestRequest, TestResponse>.Describe(null!));
    }

    #endregion

    #region Given Tests

    [Fact]
    public void Given_ReturnsScenarioForChaining()
    {
        // Act
        var scenario = Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .Given(r => { /* no-op */ });

        // Assert
        scenario.ShouldNotBeNull();
        scenario.Description.ShouldBe("Test");
    }

    [Fact]
    public void Given_NullConfigure_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            Scenario<TestRequest, TestResponse>
                .Describe("Test")
                .Given(null!));
    }

    [Fact]
    public async Task Given_AccumulatesModifications()
    {
        // Arrange & Act
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Accumulated Given")
            .Given(r => { /* first modification */ })
            .Given(r => { /* second modification */ })
            .UsingHandler(() => new SuccessHandler())
            .WhenAsync(new TestRequest("initial"));

        // Assert
        result.ShouldBeSuccess();
    }

    #endregion

    #region UsingHandler Tests

    [Fact]
    public void UsingHandler_ReturnsScenarioForChaining()
    {
        // Act
        var scenario = Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .UsingHandler(() => new SuccessHandler());

        // Assert
        scenario.ShouldNotBeNull();
    }

    [Fact]
    public void UsingHandler_NullFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            Scenario<TestRequest, TestResponse>
                .Describe("Test")
                .UsingHandler(null!));
    }

    #endregion

    #region WhenAsync Tests

    [Fact]
    public async Task WhenAsync_WithoutHandler_ThrowsInvalidOperationException()
    {
        // Arrange
        var scenario = Scenario<TestRequest, TestResponse>
            .Describe("Test without handler");

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await scenario.WhenAsync(new TestRequest("test")));

        ex.Message.ShouldContain("UsingHandler()");
    }

    [Fact]
    public async Task WhenAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var scenario = Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .UsingHandler(() => new SuccessHandler());

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await scenario.WhenAsync(null!));
    }

    [Fact]
    public async Task WhenAsync_SuccessfulHandler_ReturnsSuccessResult()
    {
        // Act
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Success scenario")
            .UsingHandler(() => new SuccessHandler())
            .WhenAsync(new TestRequest("hello"));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.ShouldBeSuccess(r => r.Result.ShouldBe(5));
    }

    [Fact]
    public async Task WhenAsync_ErrorHandler_ReturnsErrorResult()
    {
        // Act
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Error scenario")
            .UsingHandler(() => new ErrorHandler())
            .WhenAsync(new TestRequest("test"));

        // Assert
        result.IsError.ShouldBeTrue();
        result.ShouldBeError(e => e.Message.ShouldContain("Error"));
    }

    [Fact]
    public async Task WhenAsync_ThrowingHandler_ReturnsExceptionResult()
    {
        // Act
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Exception scenario")
            .UsingHandler(() => new ThrowingHandler())
            .WhenAsync(new TestRequest("test"));

        // Assert
        result.HasException.ShouldBeTrue();
        result.ShouldThrow<InvalidOperationException>();
    }

    #endregion

    #region ScenarioResult Success Assertions

    [Fact]
    public async Task ShouldBeSuccess_WhenSuccess_ReturnsValue()
    {
        // Arrange
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .UsingHandler(() => new SuccessHandler())
            .WhenAsync(new TestRequest("test"));

        // Act
        var value = result.ShouldBeSuccess();

        // Assert
        value.Result.ShouldBe(4);
    }

    [Fact]
    public async Task ShouldBeSuccess_WithValidator_ExecutesValidator()
    {
        // Arrange
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .UsingHandler(() => new SuccessHandler())
            .WhenAsync(new TestRequest("test"));

        var validated = false;

        // Act
        result.ShouldBeSuccess(r =>
        {
            r.Result.ShouldBe(4);
            validated = true;
        });

        // Assert
        validated.ShouldBeTrue();
    }

    [Fact]
    public async Task ShouldBeSuccess_WhenError_Throws()
    {
        // Arrange
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .UsingHandler(() => new ErrorHandler())
            .WhenAsync(new TestRequest("test"));

        // Act & Assert
        Should.Throw<Xunit.Sdk.TrueException>(() => result.ShouldBeSuccess());
    }

    [Fact]
    public async Task ShouldBeSuccessAnd_ReturnsAndConstraint()
    {
        // Arrange
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .UsingHandler(() => new SuccessHandler())
            .WhenAsync(new TestRequest("test"));

        // Act
        var constraint = result.ShouldBeSuccessAnd();

        // Assert
        constraint.Value.Result.ShouldBe(4);
    }

    #endregion

    #region ScenarioResult Error Assertions

    [Fact]
    public async Task ShouldBeError_WhenError_ReturnsError()
    {
        // Arrange
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .UsingHandler(() => new ErrorHandler())
            .WhenAsync(new TestRequest("test"));

        // Act
        var error = result.ShouldBeError();

        // Assert
        error.Message.ShouldContain("Error");
    }

    [Fact]
    public async Task ShouldBeError_WithValidator_ExecutesValidator()
    {
        // Arrange
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .UsingHandler(() => new ErrorHandler())
            .WhenAsync(new TestRequest("test"));

        var validated = false;

        // Act
        result.ShouldBeError(e =>
        {
            e.Message.ShouldContain("test");
            validated = true;
        });

        // Assert
        validated.ShouldBeTrue();
    }

    [Fact]
    public async Task ShouldBeError_WhenSuccess_Throws()
    {
        // Arrange
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .UsingHandler(() => new SuccessHandler())
            .WhenAsync(new TestRequest("test"));

        // Act & Assert
        Should.Throw<Xunit.Sdk.TrueException>(() => result.ShouldBeError());
    }

    [Fact]
    public async Task ShouldBeErrorAnd_ReturnsAndConstraint()
    {
        // Arrange
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .UsingHandler(() => new ErrorHandler())
            .WhenAsync(new TestRequest("test"));

        // Act
        var constraint = result.ShouldBeErrorAnd();

        // Assert
        constraint.Value.Message.ShouldContain("Error");
    }

    [Fact]
    public async Task ShouldBeValidationError_WhenValidationError_ReturnsError()
    {
        // Arrange
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .UsingHandler(() => new ValidationErrorHandler("Email"))
            .WhenAsync(new TestRequest("test"));

        // Act
        var error = result.ShouldBeValidationError("Email");

        // Assert
        error.Message.ShouldContain("Email");
    }

    [Fact]
    public async Task ShouldBeErrorWithCode_MatchingCode_ReturnsError()
    {
        // Arrange
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .UsingHandler(() => new ErrorHandler("custom.code"))
            .WhenAsync(new TestRequest("test"));

        // Act
        var error = result.ShouldBeErrorWithCode("custom.code");

        // Assert
        error.Message.ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region ScenarioResult Exception Assertions

    [Fact]
    public async Task ShouldThrow_WhenException_ReturnsException()
    {
        // Arrange
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .UsingHandler(() => new ThrowingHandler())
            .WhenAsync(new TestRequest("test"));

        // Act
        var exception = result.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.Message.ShouldContain("Crashed");
    }

    [Fact]
    public async Task ShouldThrow_WrongExceptionType_Throws()
    {
        // Arrange
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .UsingHandler(() => new ThrowingHandler())
            .WhenAsync(new TestRequest("test"));

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() =>
            result.ShouldThrow<ArgumentException>());
        ex.Message.ShouldContain("InvalidOperationException");
    }

    [Fact]
    public async Task ShouldThrow_NoException_Throws()
    {
        // Arrange
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .UsingHandler(() => new SuccessHandler())
            .WhenAsync(new TestRequest("test"));

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() =>
            result.ShouldThrow<Exception>());
        ex.Message.ShouldContain("no exception was thrown");
    }

    [Fact]
    public async Task ShouldThrow_WithValidator_ExecutesValidator()
    {
        // Arrange
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .UsingHandler(() => new ThrowingHandler())
            .WhenAsync(new TestRequest("test"));

        var validated = false;

        // Act
        result.ShouldThrow<InvalidOperationException>(ex =>
        {
            ex.Message.ShouldContain("test");
            validated = true;
        });

        // Assert
        validated.ShouldBeTrue();
    }

    [Fact]
    public async Task ShouldThrowAnd_ReturnsAndConstraint()
    {
        // Arrange
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .UsingHandler(() => new ThrowingHandler())
            .WhenAsync(new TestRequest("test"));

        // Act
        var constraint = result.ShouldThrowAnd<InvalidOperationException>();

        // Assert
        constraint.Value.Message.ShouldContain("Crashed");
    }

    #endregion

    #region ScenarioResult Properties

    [Fact]
    public async Task IsSuccess_WhenSuccess_ReturnsTrue()
    {
        // Arrange
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .UsingHandler(() => new SuccessHandler())
            .WhenAsync(new TestRequest("test"));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsError.ShouldBeFalse();
        result.HasException.ShouldBeFalse();
    }

    [Fact]
    public async Task IsError_WhenError_ReturnsTrue()
    {
        // Arrange
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .UsingHandler(() => new ErrorHandler())
            .WhenAsync(new TestRequest("test"));

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsError.ShouldBeTrue();
        result.HasException.ShouldBeFalse();
    }

    [Fact]
    public async Task HasException_WhenException_ReturnsTrue()
    {
        // Arrange
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .UsingHandler(() => new ThrowingHandler())
            .WhenAsync(new TestRequest("test"));

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsError.ShouldBeFalse();
        result.HasException.ShouldBeTrue();
    }

    [Fact]
    public async Task Result_WhenSuccess_ReturnsEither()
    {
        // Arrange
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .UsingHandler(() => new SuccessHandler())
            .WhenAsync(new TestRequest("test"));

        // Act
        var either = result.Result;

        // Assert
        either.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Result_WhenException_Throws()
    {
        // Arrange
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .UsingHandler(() => new ThrowingHandler())
            .WhenAsync(new TestRequest("test"));

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => _ = result.Result);
        ex.Message.ShouldContain("threw an exception");
    }

    [Fact]
    public async Task ImplicitConversion_WhenSuccess_ReturnsEither()
    {
        // Arrange
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Test")
            .UsingHandler(() => new SuccessHandler())
            .WhenAsync(new TestRequest("test"));

        // Act
        Either<EncinaError, TestResponse> either = result;

        // Assert
        either.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Full Scenario Examples

    [Fact]
    public async Task FullScenario_SuccessPath()
    {
        // This demonstrates a complete scenario test
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Create order with premium customer")
            .Given(r => { /* could modify request if mutable */ })
            .UsingHandler(() => new SuccessHandler())
            .WhenAsync(new TestRequest("PREMIUM-CUSTOMER"));

        result.ShouldBeSuccessAnd()
            .ShouldSatisfy(r => r.Result.ShouldBe(16));
    }

    [Fact]
    public async Task FullScenario_ErrorPath()
    {
        // This demonstrates a complete error scenario test
        var result = await Scenario<TestRequest, TestResponse>
            .Describe("Reject empty customer ID")
            .UsingHandler(() => new ValidationErrorHandler("CustomerId"))
            .WhenAsync(new TestRequest(""));

        result.ShouldBeValidationError("CustomerId");
    }

    #endregion
}
