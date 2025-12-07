using System;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using Shouldly;
using SimpleMediator;

namespace SimpleMediator.Tests;

public sealed class TypeExtensionsTests
{
    [Fact]
    public void IsAssignableFromGeneric_ReturnsFalseForNullCandidate()
    {
        typeof(IPipelineBehavior<,>).IsAssignableFromGeneric(null!).ShouldBeFalse();
    }

    [Fact]
    public void IsAssignableFromGeneric_DetectsOpenGenericImplementation()
    {
        typeof(IPipelineBehavior<,>).IsAssignableFromGeneric(typeof(InstrumentationPipeline<,>)).ShouldBeTrue();
    }

    [Fact]
    public void IsAssignableFromGeneric_UsesStandardAssignableForNonGenericInterfaces()
    {
        typeof(IDisposable).IsAssignableFromGeneric(typeof(SampleDisposable)).ShouldBeTrue();
    }

    private sealed class InstrumentationPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public Task<Either<Error, TResponse>> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
            => next();
    }

    private sealed class SampleDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
