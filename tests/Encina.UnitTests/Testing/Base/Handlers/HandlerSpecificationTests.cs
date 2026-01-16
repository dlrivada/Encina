using Encina.Testing.Handlers;
using Encina.UnitTests.Testing.Base.TestFixtures;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Testing.Base.Handlers;

public sealed class HandlerSpecificationTests
{
    #region Test Infrastructure

    // Base test spec with public accessors for testing
    private sealed class TestableSpec : HandlerSpecification<TestRequest, TestResponse>
    {
        private readonly Func<IRequestHandler<TestRequest, TestResponse>> _handlerFactory;
        private readonly string _defaultValue;

        public TestableSpec(
            Func<IRequestHandler<TestRequest, TestResponse>> handlerFactory,
            string defaultValue = "test")
        {
            _handlerFactory = handlerFactory;
            _defaultValue = defaultValue;
        }

        protected override TestRequest CreateRequest() => new(_defaultValue);
        protected override IRequestHandler<TestRequest, TestResponse> CreateHandler() => _handlerFactory();

        // Public accessors for testing
        public void CallGiven(Action<TestRequest> configure) => Given(configure);
        public void CallGivenRequest(TestRequest request) => GivenRequest(request);
        public Task CallWhen(CancellationToken ct = default) => When(ct);
        public Task CallWhenWithModify(Action<TestRequest> modify, CancellationToken ct = default) => When(modify, ct);
        public TestResponse CallThenSuccess(Action<TestResponse>? validate = null) => ThenSuccess(validate);
        public global::Encina.Testing.AndConstraint<TestResponse> CallThenSuccessAnd() => ThenSuccessAnd();
        public EncinaError CallThenError(Action<EncinaError>? validate = null) => ThenError(validate);
        public global::Encina.Testing.AndConstraint<EncinaError> CallThenErrorAnd() => ThenErrorAnd();
        public EncinaError CallThenValidationError(params string[] properties) => ThenValidationError(properties);
        public EncinaError CallThenErrorWithCode(string code) => ThenErrorWithCode(code);
        public TException CallThenThrows<TException>() where TException : Exception => ThenThrows<TException>();
        public TException CallThenThrows<TException>(Action<TException> validate) where TException : Exception =>
            ThenThrows(validate);
        public global::Encina.Testing.AndConstraint<TException> CallThenThrowsAnd<TException>() where TException : Exception =>
            ThenThrowsAnd<TException>();
    }

    #endregion

    #region Given Tests

    [Fact]
    public void Given_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new TestableSpec(() => new SuccessHandler());

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => spec.CallGiven(null!));
    }

    [Fact]
    public void GivenRequest_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new TestableSpec(() => new SuccessHandler());

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => spec.CallGivenRequest(null!));
    }

    #endregion

    #region When Tests

    [Fact]
    public async Task When_ExecutesHandler()
    {
        // Arrange
        var spec = new TestableSpec(() => new SuccessHandler());

        // Act
        await spec.CallWhen();

        // Assert
        spec.CallThenSuccess().Result.ShouldBe(4); // "test" has 4 characters
    }

    [Fact]
    public async Task When_WithGiven_AppliesModifications()
    {
        // Arrange
        var spec = new TestableSpec(() => new SuccessHandler());
        spec.CallGiven(r => { }); // Given does nothing but accumulates

        // Act
        await spec.CallWhen();

        // Assert
        spec.CallThenSuccess().Result.ShouldBe(4);
    }

    [Fact]
    public async Task When_NullModify_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new TestableSpec(() => new SuccessHandler());

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await spec.CallWhenWithModify(null!));
    }

    #endregion

    #region ThenSuccess Tests

    [Fact]
    public async Task ThenSuccess_WhenHandlerSucceeds_ReturnsValue()
    {
        // Arrange
        var spec = new TestableSpec(() => new SuccessHandler());
        await spec.CallWhen();

        // Act
        var result = spec.CallThenSuccess();

        // Assert
        result.Result.ShouldBe(4);
    }

    [Fact]
    public async Task ThenSuccess_WithValidator_ExecutesValidator()
    {
        // Arrange
        var spec = new TestableSpec(() => new SuccessHandler());
        await spec.CallWhen();
        var validated = false;

        // Act
        spec.CallThenSuccess(r =>
        {
            r.Result.ShouldBe(4);
            validated = true;
        });

        // Assert
        validated.ShouldBeTrue();
    }

    [Fact]
    public async Task ThenSuccess_WhenHandlerReturnsError_Throws()
    {
        // Arrange
        var spec = new TestableSpec(() => new ErrorHandler());
        await spec.CallWhen();

        // Act & Assert
        Should.Throw<Xunit.Sdk.TrueException>(() => spec.CallThenSuccess());
    }

    [Fact]
    public void ThenSuccess_BeforeWhen_ThrowsInvalidOperationException()
    {
        // Arrange
        var spec = new TestableSpec(() => new SuccessHandler());

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => spec.CallThenSuccess());
        ex.Message.ShouldContain("When()");
    }

    [Fact]
    public async Task ThenSuccessAnd_ReturnsAndConstraint()
    {
        // Arrange
        var spec = new TestableSpec(() => new SuccessHandler());
        await spec.CallWhen();

        // Act
        var constraint = spec.CallThenSuccessAnd();

        // Assert
        constraint.Value.Result.ShouldBe(4);
    }

    #endregion

    #region ThenError Tests

    [Fact]
    public async Task ThenError_WhenHandlerReturnsError_ReturnsError()
    {
        // Arrange
        var spec = new TestableSpec(() => new ErrorHandler());
        await spec.CallWhen();

        // Act
        var error = spec.CallThenError();

        // Assert
        error.Message.ShouldContain("Error processing");
    }

    [Fact]
    public async Task ThenError_WithValidator_ExecutesValidator()
    {
        // Arrange
        var spec = new TestableSpec(() => new ErrorHandler());
        await spec.CallWhen();
        var validated = false;

        // Act
        spec.CallThenError(e =>
        {
            e.Message.ShouldContain("test");
            validated = true;
        });

        // Assert
        validated.ShouldBeTrue();
    }

    [Fact]
    public async Task ThenError_WhenHandlerSucceeds_Throws()
    {
        // Arrange
        var spec = new TestableSpec(() => new SuccessHandler());
        await spec.CallWhen();

        // Act & Assert
        Should.Throw<Xunit.Sdk.TrueException>(() => spec.CallThenError());
    }

    [Fact]
    public async Task ThenErrorAnd_ReturnsAndConstraint()
    {
        // Arrange
        var spec = new TestableSpec(() => new ErrorHandler());
        await spec.CallWhen();

        // Act
        var constraint = spec.CallThenErrorAnd();

        // Assert
        constraint.Value.Message.ShouldContain("Error");
    }

    [Fact]
    public async Task ThenErrorWithCode_MatchingCode_ReturnsError()
    {
        // Arrange
        var spec = new TestableSpec(() => new ErrorHandler("custom.code"));
        await spec.CallWhen();

        // Act
        var error = spec.CallThenErrorWithCode("custom.code");

        // Assert
        error.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task ThenErrorWithCode_DifferentCode_Throws()
    {
        // Arrange
        var spec = new TestableSpec(() => new ErrorHandler("actual.code"));
        await spec.CallWhen();

        // Act & Assert
        Should.Throw<Xunit.Sdk.EqualException>(() =>
            spec.CallThenErrorWithCode("expected.code"));
    }

    #endregion

    #region ThenValidationError Tests

    [Fact]
    public async Task ThenValidationError_WhenValidationError_ReturnsError()
    {
        // Arrange
        var spec = new TestableSpec(() => new ValidationErrorHandler("Email"));
        await spec.CallWhen();

        // Act
        var error = spec.CallThenValidationError("Email");

        // Assert
        error.Message.ShouldContain("Email");
    }

    [Fact]
    public async Task ThenValidationError_MultipleProperties_ValidatesAll()
    {
        // Arrange
        var spec = new TestableSpec(() => new MultiPropertyValidationErrorHandler());
        await spec.CallWhen();

        // Act
        var error = spec.CallThenValidationError("Email", "Name");

        // Assert
        error.Message.ShouldContain("Email");
        error.Message.ShouldContain("Name");
    }

    [Fact]
    public async Task ThenValidationError_WhenNotValidationError_Throws()
    {
        // Arrange
        var spec = new TestableSpec(() => new ErrorHandler());
        await spec.CallWhen();

        // Act & Assert
        Should.Throw<Xunit.Sdk.TrueException>(() => spec.CallThenValidationError("Email"));
    }

    [Fact]
    public async Task ThenValidationError_EmptyPropertyNames_ThrowsArgumentException()
    {
        // Arrange
        var spec = new TestableSpec(() => new ValidationErrorHandler("Email"));
        await spec.CallWhen();

        // Act & Assert
        Should.Throw<ArgumentException>(() => spec.CallThenValidationError());
    }

    #endregion

    #region ThenThrows Tests

    [Fact]
    public async Task ThenThrows_WhenExceptionThrown_ReturnsException()
    {
        // Arrange
        var spec = new TestableSpec(() => new ThrowingHandler());
        await spec.CallWhen();

        // Act
        var exception = spec.CallThenThrows<InvalidOperationException>();

        // Assert
        exception.Message.ShouldContain("Handler crashed");
    }

    [Fact]
    public async Task ThenThrows_WrongExceptionType_Throws()
    {
        // Arrange
        var spec = new TestableSpec(() => new ThrowingHandler());
        await spec.CallWhen();

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() =>
            spec.CallThenThrows<ArgumentException>());
        ex.Message.ShouldContain("InvalidOperationException");
    }

    [Fact]
    public async Task ThenThrows_NoExceptionThrown_Throws()
    {
        // Arrange
        var spec = new TestableSpec(() => new SuccessHandler());
        await spec.CallWhen();

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() =>
            spec.CallThenThrows<Exception>());
        ex.Message.ShouldContain("no exception was thrown");
    }

    [Fact]
    public async Task ThenThrows_WithValidator_ExecutesValidator()
    {
        // Arrange
        var spec = new TestableSpec(() => new ThrowingHandler());
        await spec.CallWhen();
        var validated = false;

        // Act
        spec.CallThenThrows<InvalidOperationException>(ex =>
        {
            ex.Message.ShouldContain("test");
            validated = true;
        });

        // Assert
        validated.ShouldBeTrue();
    }

    [Fact]
    public async Task ThenThrowsAnd_ReturnsAndConstraint()
    {
        // Arrange
        var spec = new TestableSpec(() => new ThrowingHandler());
        await spec.CallWhen();

        // Act
        var constraint = spec.CallThenThrowsAnd<InvalidOperationException>();

        // Assert
        constraint.Value.Message.ShouldContain("crashed");
    }

    #endregion

    #region Exception During When Tests

    [Fact]
    public async Task ThenSuccess_WhenExceptionThrown_ThrowsInvalidOperationException()
    {
        // Arrange
        var spec = new TestableSpec(() => new ThrowingHandler());
        await spec.CallWhen();

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => spec.CallThenSuccess());
        ex.Message.ShouldContain("ThenThrows<T>()");
    }

    [Fact]
    public async Task ThenError_WhenExceptionThrown_ThrowsInvalidOperationException()
    {
        // Arrange
        var spec = new TestableSpec(() => new ThrowingHandler());
        await spec.CallWhen();

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => spec.CallThenError());
        ex.Message.ShouldContain("ThenThrows<T>()");
    }

    #endregion
}
