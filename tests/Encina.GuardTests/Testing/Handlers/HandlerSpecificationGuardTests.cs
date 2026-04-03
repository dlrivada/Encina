using Encina.Testing.Handlers;

namespace Encina.GuardTests.Testing.Handlers;

/// <summary>
/// Guard tests for <see cref="HandlerSpecification{TRequest, TResponse}"/>
/// and <see cref="ScenarioResult{TResponse}"/>.
/// </summary>
public class HandlerSpecificationGuardTests
{
    #region HandlerSpecification Guards

    public class GivenGuards
    {
        [Fact]
        public void NullConfigure_Throws()
        {
            var spec = new TestHandlerSpec();

            Should.Throw<ArgumentNullException>(() =>
                spec.CallGiven(null!));
        }
    }

    public class GivenRequestGuards
    {
        [Fact]
        public void NullRequest_Throws()
        {
            var spec = new TestHandlerSpec();

            Should.Throw<ArgumentNullException>(() =>
                spec.CallGivenRequest(null!));
        }
    }

    public class WhenWithModifyGuards
    {
        [Fact]
        public async Task NullModify_Throws()
        {
            var spec = new TestHandlerSpec();

            await Should.ThrowAsync<ArgumentNullException>(async () =>
                await spec.CallWhenWithModify(null!));
        }
    }

    public class ThenValidationErrorGuards
    {
        [Fact]
        public async Task NullPropertyNames_Throws()
        {
            var spec = new TestHandlerSpec();
            spec.ReturnError = true;
            await spec.CallWhen();

            Should.Throw<ArgumentNullException>(() =>
                spec.CallThenValidationError(null!));
        }

        [Fact]
        public async Task EmptyPropertyNames_Throws()
        {
            var spec = new TestHandlerSpec();
            spec.ReturnError = true;
            await spec.CallWhen();

            Should.Throw<ArgumentException>(() =>
                spec.CallThenValidationError(Array.Empty<string>()));
        }
    }

    public class ThenErrorWithCodeGuards
    {
        [Fact]
        public async Task NullCode_Throws()
        {
            var spec = new TestHandlerSpec();
            spec.ReturnError = true;
            await spec.CallWhen();

            Should.Throw<ArgumentNullException>(() =>
                spec.CallThenErrorWithCode(null!));
        }
    }

    public class ThenThrowsGuards
    {
        [Fact]
        public async Task NullValidator_Throws()
        {
            var spec = new TestHandlerSpec();
            spec.ThrowOnHandle = true;
            await spec.CallWhen();

            Should.Throw<ArgumentNullException>(() =>
                spec.CallThenThrowsWithValidate(null!));
        }
    }

    public class ThenBeforeWhenGuards
    {
        [Fact]
        public void Result_BeforeWhen_Throws()
        {
            var spec = new TestHandlerSpec();

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = spec.GetResult();
            });
        }

        [Fact]
        public void Request_BeforeWhen_Throws()
        {
            var spec = new TestHandlerSpec();

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = spec.GetRequest();
            });
        }

        [Fact]
        public void ThenSuccess_BeforeWhen_Throws()
        {
            var spec = new TestHandlerSpec();

            Should.Throw<InvalidOperationException>(() =>
                spec.CallThenSuccess());
        }

        [Fact]
        public void ThenError_BeforeWhen_Throws()
        {
            var spec = new TestHandlerSpec();

            Should.Throw<InvalidOperationException>(() =>
                spec.CallThenError());
        }

        [Fact]
        public void ThenThrows_BeforeWhen_Throws()
        {
            var spec = new TestHandlerSpec();

            Should.Throw<InvalidOperationException>(() =>
                spec.CallThenThrows<InvalidOperationException>());
        }
    }

    #endregion

    #region ScenarioResult Guards

    public class ScenarioResultGuards
    {
        [Fact]
        public void ShouldBeValidationError_NullPropertyNames_Throws()
        {
            var result = CreateErrorResult();

            Should.Throw<ArgumentNullException>(() =>
                result.ShouldBeValidationError(null!));
        }

        [Fact]
        public void ShouldBeValidationError_EmptyPropertyNames_Throws()
        {
            var result = CreateErrorResult();

            Should.Throw<ArgumentException>(() =>
                result.ShouldBeValidationError(Array.Empty<string>()));
        }

        [Fact]
        public void ShouldBeErrorWithCode_NullCode_Throws()
        {
            var result = CreateErrorResult();

            Should.Throw<ArgumentNullException>(() =>
                result.ShouldBeErrorWithCode(null!));
        }

        [Fact]
        public void ShouldThrow_WithValidator_NullValidator_Throws()
        {
            var result = CreateExceptionResult();

            Should.Throw<ArgumentNullException>(() =>
                result.ShouldThrow<InvalidOperationException>(null!));
        }

        [Fact]
        public void ShouldBeSuccess_WithException_Throws()
        {
            var result = CreateExceptionResult();

            Should.Throw<InvalidOperationException>(() =>
                result.ShouldBeSuccess());
        }

        [Fact]
        public void ShouldBeError_WithException_Throws()
        {
            var result = CreateExceptionResult();

            Should.Throw<InvalidOperationException>(() =>
                result.ShouldBeError());
        }

        [Fact]
        public void Result_WithException_Throws()
        {
            var result = CreateExceptionResult();

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = result.Result;
            });
        }

        [Fact]
        public void ShouldThrow_NoException_Throws()
        {
            var eitherResult = LanguageExt.Prelude.Right<EncinaError, string>("ok");
            var result = CreateSuccessResult(eitherResult);

            Should.Throw<InvalidOperationException>(() =>
                result.ShouldThrow<InvalidOperationException>());
        }

        private static ScenarioResult<string> CreateErrorResult()
        {
            var error = LanguageExt.Prelude.Left<EncinaError, string>(
                EncinaErrors.Create("test.error", "Test error"));
            // Use internal constructor via reflection
            var ctor = typeof(ScenarioResult<string>).GetConstructors(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .First(c => c.GetParameters().Length == 3 &&
                    c.GetParameters()[0].ParameterType == typeof(LanguageExt.Either<EncinaError, string>));
            return (ScenarioResult<string>)ctor.Invoke([error, "request", "test scenario"]);
        }

        private static ScenarioResult<string> CreateExceptionResult()
        {
            var ctor = typeof(ScenarioResult<string>).GetConstructors(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .First(c => c.GetParameters().Length == 3 &&
                    c.GetParameters()[0].ParameterType == typeof(Exception));
            return (ScenarioResult<string>)ctor.Invoke([new InvalidOperationException("test"), "request", "test scenario"]);
        }

        private static ScenarioResult<string> CreateSuccessResult(LanguageExt.Either<EncinaError, string> result)
        {
            var ctor = typeof(ScenarioResult<string>).GetConstructors(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .First(c => c.GetParameters().Length == 3 &&
                    c.GetParameters()[0].ParameterType == typeof(LanguageExt.Either<EncinaError, string>));
            return (ScenarioResult<string>)ctor.Invoke([result, "request", "test scenario"]);
        }
    }

    #endregion

    // Test handler types
    private sealed record TestRequest : IRequest<string>
    {
        public string CustomerId { get; set; } = "CUST-001";
    }

    private sealed class TestHandler : IRequestHandler<TestRequest, string>
    {
        public bool ThrowOnHandle { get; set; }
        public bool ReturnError { get; set; }

        public Task<LanguageExt.Either<EncinaError, string>> Handle(TestRequest request, CancellationToken ct)
        {
            if (ThrowOnHandle)
                throw new InvalidOperationException("Test exception");

            if (ReturnError)
                return Task.FromResult<LanguageExt.Either<EncinaError, string>>(
                    LanguageExt.Prelude.Left<EncinaError, string>(
                        EncinaErrors.Create("test.error", "Test error")));

            return Task.FromResult<LanguageExt.Either<EncinaError, string>>(
                LanguageExt.Prelude.Right<EncinaError, string>("result"));
        }
    }

    private sealed class TestHandlerSpec : HandlerSpecification<TestRequest, string>
    {
        private readonly TestHandler _handler = new();

        public bool ThrowOnHandle
        {
            get => _handler.ThrowOnHandle;
            set => _handler.ThrowOnHandle = value;
        }

        public bool ReturnError
        {
            get => _handler.ReturnError;
            set => _handler.ReturnError = value;
        }

        protected override TestRequest CreateRequest() => new();
        protected override IRequestHandler<TestRequest, string> CreateHandler() => _handler;

        // Expose protected members for testing
        public void CallGiven(Action<TestRequest> configure) => Given(configure);
        public void CallGivenRequest(TestRequest request) => GivenRequest(request);
        public Task CallWhen() => When();
        public Task CallWhenWithModify(Action<TestRequest> modify) => When(modify);
        public string CallThenSuccess() => ThenSuccess();
        public EncinaError CallThenError() => ThenError();
        public EncinaError CallThenValidationError(params string[] propertyNames) => ThenValidationError(propertyNames);
        public EncinaError CallThenErrorWithCode(string code) => ThenErrorWithCode(code);
        public TException CallThenThrows<TException>() where TException : Exception => ThenThrows<TException>();
        public void CallThenThrowsWithValidate(Action<InvalidOperationException> validate) => ThenThrows(validate);
        public LanguageExt.Either<EncinaError, string> GetResult() => Result;
        public TestRequest GetRequest() => Request;
    }
}
