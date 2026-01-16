using Encina.Testing;
using System.Threading;
using System.Threading.Tasks;
using Encina;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Testing.Base.TestFixtures;

public sealed record TestRequest(string Value) : IRequest<TestResponse>;
public sealed record TestResponse(int Result);

public sealed class SuccessHandler : IRequestHandler<TestRequest, TestResponse>
{
    public Task<Either<EncinaError, TestResponse>> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult<Either<EncinaError, TestResponse>>(Right(new TestResponse(request.Value.Length)));
    }
}

public sealed class ErrorHandler : IRequestHandler<TestRequest, TestResponse>
{
    private readonly string _errorCode;

    public ErrorHandler(string errorCode = "test.error")
    {
        _errorCode = errorCode;
    }

    public Task<Either<EncinaError, TestResponse>> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult<Either<EncinaError, TestResponse>>(
            EncinaErrors.Create(_errorCode, $"Error processing: {request.Value}"));
    }
}

public sealed class ThrowingHandler : IRequestHandler<TestRequest, TestResponse>
{
    public Task<Either<EncinaError, TestResponse>> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException($"Handler crashed: {request.Value}");
    }
}

public sealed class ValidationErrorHandler : IRequestHandler<TestRequest, TestResponse>
{
    private readonly string _propertyName;

    public ValidationErrorHandler(string propertyName)
    {
        _propertyName = propertyName;
    }

    public Task<Either<EncinaError, TestResponse>> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult<Either<EncinaError, TestResponse>>(
            EncinaErrors.Create("encina.validation.required", $"The field '{_propertyName}' is required"));
    }
}

public sealed class MultiPropertyValidationErrorHandler : IRequestHandler<TestRequest, TestResponse>
{
    public Task<Either<EncinaError, TestResponse>> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult<Either<EncinaError, TestResponse>>(
            EncinaErrors.Create("encina.validation.multiple", "Fields 'Email' and 'Name' are required"));
    }
}
