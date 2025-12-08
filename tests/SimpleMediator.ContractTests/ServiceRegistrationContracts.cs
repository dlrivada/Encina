using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using SimpleMediator;

namespace SimpleMediator.ContractTests;

public sealed class ServiceRegistrationContracts
{
    private static readonly Type[] PipelineBehaviors =
    {
        typeof(global::SimpleMediator.CommandActivityPipelineBehavior<,>),
        typeof(global::SimpleMediator.CommandMetricsPipelineBehavior<,>),
        typeof(global::SimpleMediator.QueryActivityPipelineBehavior<,>),
        typeof(global::SimpleMediator.QueryMetricsPipelineBehavior<,>)
    };

    [Fact]
    public void DefaultRegistrationRegistersBehaviorsOnce()
    {
        var services = new ServiceCollection();

        services.AddSimpleMediator(typeof(global::SimpleMediator.SimpleMediator).Assembly);

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

        services.AddSimpleMediator(configure: cfg =>
        {
            cfg.AddPipelineBehavior(typeof(SamplePipelineBehavior<,>));
        }, typeof(global::SimpleMediator.SimpleMediator).Assembly);

        var descriptors = services.Where(IsPipelineDescriptor).ToList();
        descriptors.Count.ShouldBe(PipelineBehaviors.Length + 1);
        descriptors.ShouldContain(d => ImplementationMatches(d, typeof(SamplePipelineBehavior<,>)));
    }

    [Fact]
    public void CommandPipelineConfigurationRegistersSpecializedDescriptor()
    {
        var services = new ServiceCollection();

        services.AddSimpleMediator(configure: cfg =>
        {
            cfg.AddPipelineBehavior(typeof(SampleCommandPipelineBehavior<,>));
        }, typeof(global::SimpleMediator.SimpleMediator).Assembly);

        services.ShouldContain(d =>
            IsPipelineDescriptor(d)
            && ImplementationMatches(d, typeof(SampleCommandPipelineBehavior<,>))
            && d.Lifetime == ServiceLifetime.Scoped);

        services.ShouldContain(d =>
            d.ServiceType == typeof(global::SimpleMediator.ICommandPipelineBehavior<,>)
            && ImplementationMatches(d, typeof(SampleCommandPipelineBehavior<,>))
            && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void QueryPipelineConfigurationRegistersSpecializedDescriptor()
    {
        var services = new ServiceCollection();

        services.AddSimpleMediator(configure: cfg =>
        {
            cfg.AddPipelineBehavior(typeof(SampleQueryPipelineBehavior<,>));
        }, typeof(global::SimpleMediator.SimpleMediator).Assembly);

        services.ShouldContain(d =>
            IsPipelineDescriptor(d)
            && ImplementationMatches(d, typeof(SampleQueryPipelineBehavior<,>))
            && d.Lifetime == ServiceLifetime.Scoped);

        services.ShouldContain(d =>
            d.ServiceType == typeof(global::SimpleMediator.IQueryPipelineBehavior<,>)
            && ImplementationMatches(d, typeof(SampleQueryPipelineBehavior<,>))
            && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void ConfiguredPreProcessorRegistersScopedDescriptor()
    {
        var services = new ServiceCollection();

        services.AddSimpleMediator(configure: cfg =>
        {
            cfg.AddRequestPreProcessor(typeof(SampleRequestPreProcessor<>));
        }, typeof(global::SimpleMediator.SimpleMediator).Assembly);

        services.ShouldContain(d =>
            d.ServiceType == typeof(global::SimpleMediator.IRequestPreProcessor<>)
            && ImplementationMatches(d, typeof(SampleRequestPreProcessor<>))
            && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void ConfiguredPostProcessorRegistersScopedDescriptor()
    {
        var services = new ServiceCollection();

        services.AddSimpleMediator(configure: cfg =>
        {
            cfg.AddRequestPostProcessor(typeof(SampleRequestPostProcessor<,>));
        }, typeof(global::SimpleMediator.SimpleMediator).Assembly);

        services.ShouldContain(d =>
            d.ServiceType == typeof(global::SimpleMediator.IRequestPostProcessor<,>)
            && ImplementationMatches(d, typeof(SampleRequestPostProcessor<,>))
            && d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void DefaultRegistrationUsesNullFunctionalFailureDetector()
    {
        var services = new ServiceCollection();

        services.AddSimpleMediator(typeof(global::SimpleMediator.SimpleMediator).Assembly);

        using var provider = services.BuildServiceProvider();
        var detector = provider.GetRequiredService<global::SimpleMediator.IFunctionalFailureDetector>();

        detector.ShouldNotBeNull();
        detector.GetType().Name.ShouldBe("NullFunctionalFailureDetector");
    }

    [Fact]
    public void CustomFunctionalFailureDetectorOverridesDefault()
    {
        var services = new ServiceCollection();

        services.AddSingleton<global::SimpleMediator.IFunctionalFailureDetector, SampleFunctionalFailureDetector>();
        services.AddSimpleMediator(typeof(global::SimpleMediator.SimpleMediator).Assembly);

        using var provider = services.BuildServiceProvider();
        var detector = provider.GetRequiredService<global::SimpleMediator.IFunctionalFailureDetector>();

        detector.ShouldBeOfType<SampleFunctionalFailureDetector>();
    }

    private static bool IsPipelineDescriptor(ServiceDescriptor descriptor)
    {
        return descriptor.ServiceType.IsGenericType
               && descriptor.ServiceType.GetGenericTypeDefinition() == typeof(global::SimpleMediator.IPipelineBehavior<,>);
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

    private sealed class SamplePipelineBehavior<TRequest, TResponse> : global::SimpleMediator.IPipelineBehavior<TRequest, TResponse>
        where TRequest : global::SimpleMediator.IRequest<TResponse>
    {
        public Task<Either<Error, TResponse>> Handle(TRequest request, CancellationToken cancellationToken, global::SimpleMediator.RequestHandlerDelegate<TResponse> next)
        {
            return next();
        }
    }

    private sealed class SampleCommandPipelineBehavior<TCommand, TResponse> : global::SimpleMediator.ICommandPipelineBehavior<TCommand, TResponse>
        where TCommand : global::SimpleMediator.ICommand<TResponse>
    {
        public Task<Either<Error, TResponse>> Handle(TCommand request, CancellationToken cancellationToken, global::SimpleMediator.RequestHandlerDelegate<TResponse> next)
        {
            return next();
        }
    }

    private sealed class SampleQueryPipelineBehavior<TQuery, TResponse> : global::SimpleMediator.IQueryPipelineBehavior<TQuery, TResponse>
        where TQuery : global::SimpleMediator.IQuery<TResponse>
    {
        public Task<Either<Error, TResponse>> Handle(TQuery request, CancellationToken cancellationToken, global::SimpleMediator.RequestHandlerDelegate<TResponse> next)
        {
            return next();
        }
    }

    private sealed class SampleRequestPreProcessor<TRequest> : global::SimpleMediator.IRequestPreProcessor<TRequest>
    {
        public Task Process(TRequest request, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class SampleRequestPostProcessor<TRequest, TResponse> : global::SimpleMediator.IRequestPostProcessor<TRequest, TResponse>
    {
        public Task Process(TRequest request, Either<Error, TResponse> response, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class SampleFunctionalFailureDetector : global::SimpleMediator.IFunctionalFailureDetector
    {
        public bool TryExtractFailure(object? response, out string reason, out object? error)
        {
            reason = string.Empty;
            error = null;
            return false;
        }

        public string? TryGetErrorCode(object? error) => null;

        public string? TryGetErrorMessage(object? error) => null;
    }
}
