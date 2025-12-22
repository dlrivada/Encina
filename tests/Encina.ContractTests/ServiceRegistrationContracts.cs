using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.ContractTests;

public sealed class ServiceRegistrationContracts
{
    private static readonly Type[] PipelineBehaviors =
    {
        typeof(global::Encina.CommandActivityPipelineBehavior<,>),
        typeof(global::Encina.CommandMetricsPipelineBehavior<,>),
        typeof(global::Encina.QueryActivityPipelineBehavior<,>),
        typeof(global::Encina.QueryMetricsPipelineBehavior<,>)
    };

    [Fact]
    public void DefaultRegistrationRegistersBehaviorsOnce()
    {
        var services = new ServiceCollection();

        services.AddEncina(typeof(global::Encina.Encina).Assembly);

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
        }, typeof(global::Encina.Encina).Assembly);

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
        }, typeof(global::Encina.Encina).Assembly);

        services.ShouldContain(d =>
            IsPipelineDescriptor(d)
            && ImplementationMatches(d, typeof(SampleCommandPipelineBehavior<,>))
            && d.Lifetime == ServiceLifetime.Scoped);

        services.ShouldContain(d =>
            d.ServiceType == typeof(global::Encina.ICommandPipelineBehavior<,>)
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
        }, typeof(global::Encina.Encina).Assembly);

        services.ShouldContain(d =>
            IsPipelineDescriptor(d)
            && ImplementationMatches(d, typeof(SampleQueryPipelineBehavior<,>))
            && d.Lifetime == ServiceLifetime.Scoped);

        services.ShouldContain(d =>
            d.ServiceType == typeof(global::Encina.IQueryPipelineBehavior<,>)
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
        }, typeof(global::Encina.Encina).Assembly);

        services.ShouldContain(d =>
            d.ServiceType == typeof(global::Encina.IRequestPreProcessor<>)
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
        }, typeof(global::Encina.Encina).Assembly);

        services.ShouldContain(d =>
            d.ServiceType == typeof(global::Encina.IRequestPostProcessor<,>)
            && ImplementationMatches(d, typeof(SampleRequestPostProcessor<,>))
            && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void DefaultRegistrationUsesNullFunctionalFailureDetector()
    {
        var services = new ServiceCollection();

        services.AddEncina(typeof(global::Encina.Encina).Assembly);

        using var provider = services.BuildServiceProvider();
        var detector = provider.GetRequiredService<global::Encina.IFunctionalFailureDetector>();

        detector.ShouldNotBeNull();
        detector.GetType().Name.ShouldBe("NullFunctionalFailureDetector");
    }

    [Fact]
    public void CustomFunctionalFailureDetectorOverridesDefault()
    {
        var services = new ServiceCollection();

        services.AddSingleton<global::Encina.IFunctionalFailureDetector, SampleFunctionalFailureDetector>();
        services.AddEncina(typeof(global::Encina.Encina).Assembly);

        using var provider = services.BuildServiceProvider();
        var detector = provider.GetRequiredService<global::Encina.IFunctionalFailureDetector>();

        detector.ShouldBeOfType<SampleFunctionalFailureDetector>();
    }

    private static bool IsPipelineDescriptor(ServiceDescriptor descriptor)
    {
        return descriptor.ServiceType.IsGenericType
               && descriptor.ServiceType.GetGenericTypeDefinition() == typeof(global::Encina.IPipelineBehavior<,>);
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

    private sealed class SamplePipelineBehavior<TRequest, TResponse> : global::Encina.IPipelineBehavior<TRequest, TResponse>
        where TRequest : global::Encina.IRequest<TResponse>
    {
        public ValueTask<Either<MediatorError, TResponse>> Handle(TRequest request, global::Encina.IRequestContext context, global::Encina.RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    private sealed class SampleCommandPipelineBehavior<TCommand, TResponse> : global::Encina.ICommandPipelineBehavior<TCommand, TResponse>
        where TCommand : global::Encina.ICommand<TResponse>
    {
        public ValueTask<Either<MediatorError, TResponse>> Handle(TCommand request, global::Encina.IRequestContext context, global::Encina.RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    private sealed class SampleQueryPipelineBehavior<TQuery, TResponse> : global::Encina.IQueryPipelineBehavior<TQuery, TResponse>
        where TQuery : global::Encina.IQuery<TResponse>
    {
        public ValueTask<Either<MediatorError, TResponse>> Handle(TQuery request, global::Encina.IRequestContext context, global::Encina.RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    private sealed class SampleRequestPreProcessor<TRequest> : global::Encina.IRequestPreProcessor<TRequest>
    {
        public Task Process(TRequest request, global::Encina.IRequestContext context, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class SampleRequestPostProcessor<TRequest, TResponse> : global::Encina.IRequestPostProcessor<TRequest, TResponse>
    {
        public Task Process(TRequest request, global::Encina.IRequestContext context, Either<MediatorError, TResponse> response, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class SampleFunctionalFailureDetector : global::Encina.IFunctionalFailureDetector
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
