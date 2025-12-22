using LanguageExt;
using Shouldly;

namespace Encina.Tests;

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

    [Fact]
    public void IsAssignableFromGeneric_WorksWithConcreteClasses()
    {
        typeof(object).IsAssignableFromGeneric(typeof(string)).ShouldBeTrue();
        typeof(string).IsAssignableFromGeneric(typeof(object)).ShouldBeFalse();
    }

    [Fact]
    public void IsAssignableFromGeneric_ReturnsFalseForUnrelatedTypes()
    {
        typeof(IDisposable).IsAssignableFromGeneric(typeof(string)).ShouldBeFalse();
    }

    private sealed class InstrumentationPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public ValueTask<Either<EncinaError, TResponse>> Handle(TRequest request, IRequestContext context, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    private sealed class SampleDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
