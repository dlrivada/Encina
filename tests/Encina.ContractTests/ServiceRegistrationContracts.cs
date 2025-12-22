using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.ContractTests;

public sealed class ServiceRegistrationContracts
{
    private static readonly Type[] PipelineBehaviors =
    {
        typeof(CommandActivityPipelineBehavior<,>),
        typeof(CommandMetricsPipelineBehavior<,>),
        typeof(QueryActivityPipelineBehavior<,>),
        typeof(QueryMetricsPipelineBehavior<,>)
    };

    [Fact]
    public void DefaultRegistrationRegistersBehaviorsOnce()
    {
        var services = new ServiceCollection();

        services.AddEncina(typeof(Encina).Assembly);

        var descriptors = services.Where(IsPipelineDescriptor).ToList();
        descriptors.Count.ShouldBe(PipelineBehaviors.Length, "Each pipeline behavior should be registered exactly once by default.");

        foreach (var expected in PipelineBehaviors)
        {
            descriptors.ShouldContain(d => ImplementationMatches(d, expected));
        }
    }

    [Fact]
    public void CustomConfigurationAddsPipelineWithoutRemovingDefaults()
    {
        var services = new ServiceCollection();

        services.AddEncina(configure: cfg =>
        {
            cfg.AddPipelineBehavior(typeof(SamplePipelineBehavior<,>));
        }, typeof(Encina).Assembly);

        var descriptors = services.Where(IsPipelineDescriptor).ToList();
        descriptors.Count.ShouldBe(PipelineBehaviors.Length + 1);
        descriptors.ShouldContain(d => ImplementationMatches(d, typeof(SamplePipelineBehavior<,>)));
    }

    [Fact]
    public void CommandPipelineConfigurationRegistersSpecializedDescriptor()
    {
        var services = new ServiceCollection();

        services.AddEncina(configure: cfg =>
        {
            cfg.AddPipelineBehavior(typeof(SampleCommandPipelineBehavior<,>));
        }, typeof(Encina).Assembly);

        services.ShouldContain(d =>
            IsPipelineDescriptor(d)
            && ImplementationMatches(d, typeof(SampleCommandPipelineBehavior<,>))
            && d.Lifetime == ServiceLifetime.Scoped);

        services.ShouldContain(d =>
            d.ServiceType == typeof(ICommandPipelineBehavior<,>)
            && ImplementationMatches(d, typeof(SampleCommandPipelineBehavior<,>))
            && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void QueryPipelineConfigurationRegistersSpecializedDescriptor()
    {
        var services = new ServiceCollection();

        services.AddEncina(configure: cfg =>
        {
            cfg.AddPipelineBehavior(typeof(SampleQueryPipelineBehavior<,>));
        }, typeof(Encina).Assembly);

        services.ShouldContain(d =>
            IsPipelineDescriptor(d)
            && ImplementationMatches(d, typeof(SampleQueryPipelineBehavior<,>))
            && d.Lifetime == ServiceLifetime.Scoped);

        services.ShouldContain(d =>
            d.ServiceType == typeof(IQueryPipelineBehavior<,>)
            && ImplementationMatches(d, typeof(SampleQueryPipelineBehavior<,>))
            && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void ConfiguredPreProcessorRegistersScopedDescriptor()
    {
        var services = new ServiceCollection();

        services.AddEncina(configure: cfg =>
        {
            cfg.AddRequestPreProcessor(typeof(SampleRequestPreProcessor<>));
        }, typeof(Encina).Assembly);

        services.ShouldContain(d =>
            d.ServiceType == typeof(IRequestPreProcessor<>)
            && ImplementationMatches(d, typeof(SampleRequestPreProcessor<>))
            && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void ConfiguredPostProcessorRegistersScopedDescriptor()
    {
        var services = new ServiceCollection();

        services.AddEncina(configure: cfg =>
        {
            cfg.AddRequestPostProcessor(typeof(SampleRequestPostProcessor<,>));
        }, typeof(Encina).Assembly);

        services.ShouldContain(d =>
            d.ServiceType == typeof(IRequestPostProcessor<,>)
            && ImplementationMatches(d, typeof(SampleRequestPostProcessor<,>))
            && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void DefaultRegistrationUsesNullFunctionalFailureDetector()
    {
        var services = new ServiceCollection();

        services.AddEncina(typeof(Encina).Assembly);

        using var provider = services.BuildServiceProvider();
        var detector = provider.GetRequiredService<IFunctionalFailureDetector>();

        detector.ShouldNotBeNull();
        detector.GetType().Name.ShouldBe("NullFunctionalFailureDetector");
    }

    [Fact]
    public void CustomFunctionalFailureDetectorOverridesDefault()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IFunctionalFailureDetector, SampleFunctionalFailureDetector>();
        services.AddEncina(typeof(Encina).Assembly);

        using var provider = services.BuildServiceProvider();
        var detector = provider.GetRequiredService<IFunctionalFailureDetector>();

        detector.ShouldBeOfType<SampleFunctionalFailureDetector>();
    }

    private static bool IsPipelineDescriptor(ServiceDescriptor descriptor)
    {
        return descriptor.ServiceType.IsGenericType
               && descriptor.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>);
    }

    private static bool ImplementationMatches(ServiceDescriptor descriptor, Type candidate)
    {
        var implementation = descriptor.ImplementationType ?? descriptor.ImplementationInstance?.GetType();
        if (implementation is null)
        {
            return false;
        }

        if (implementation.IsGenericTypeDefinition)
        {
            return implementation == candidate;
        }

        if (!candidate.IsGenericTypeDefinition)
        {
            return implementation == candidate;
        }

        return implementation.IsGenericType && implementation.GetGenericTypeDefinition() == candidate;
    }

    private sealed class SamplePipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public ValueTask<Either<EncinaError, TResponse>> Handle(TRequest request, IRequestContext context, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    private sealed class SampleCommandPipelineBehavior<TCommand, TResponse> : ICommandPipelineBehavior<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public ValueTask<Either<EncinaError, TResponse>> Handle(TCommand request, IRequestContext context, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    private sealed class SampleQueryPipelineBehavior<TQuery, TResponse> : IQueryPipelineBehavior<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        public ValueTask<Either<EncinaError, TResponse>> Handle(TQuery request, IRequestContext context, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    private sealed class SampleRequestPreProcessor<TRequest> : IRequestPreProcessor<TRequest>
    {
        public Task Process(TRequest request, IRequestContext context, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class SampleRequestPostProcessor<TRequest, TResponse> : IRequestPostProcessor<TRequest, TResponse>
    {
        public Task Process(TRequest request, IRequestContext context, Either<EncinaError, TResponse> response, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class SampleFunctionalFailureDetector : IFunctionalFailureDetector
    {
        public bool TryExtractFailure(object? response, out string reason, out object? capturedFailure)
        {
            reason = string.Empty;
            capturedFailure = null;
            return false;
        }

        public string? TryGetErrorCode(object? capturedFailure) => null;

        public string? TryGetErrorMessage(object? capturedFailure) => null;
    }
}
