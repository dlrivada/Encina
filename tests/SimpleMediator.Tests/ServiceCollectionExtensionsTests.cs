using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using static LanguageExt.Prelude;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SimpleMediator;
using SimpleMediator.Tests.Fixtures;

namespace SimpleMediator.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddSimpleMediator_RegistersHandlersAndDependencies()
    {
        var services = new ServiceCollection();
        services.AddSimpleMediator(typeof(PingCommand).Assembly);

        await using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IMediator>();

        var handler = provider.GetRequiredService<IRequestHandler<PingCommand, string>>();
        handler.ShouldBeOfType<PingCommandHandler>();

        var notificationHandlers = provider.GetServices<INotificationHandler<DomainNotification>>();
        notificationHandlers.Count().ShouldBe(2);
    }

    [Fact]
    public void AddSimpleMediator_UsesConfigurationForHandlersAndPipeline()
    {
        var services = new ServiceCollection();
        services.AddSimpleMediator(cfg => cfg
            .WithHandlerLifetime(ServiceLifetime.Singleton)
            .AddPipelineBehavior(typeof(ConfiguredPipelineBehavior<,>))
            .AddRequestPreProcessor(typeof(ConfiguredPreProcessor<>))
            .AddRequestPostProcessor(typeof(ConfiguredPostProcessor<,>)), typeof(PingCommand).Assembly);

        var handlerDescriptor = services.Single(d => d.ServiceType == typeof(IRequestHandler<PingCommand, string>));
        handlerDescriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);

        services.ShouldContain(d => d.ImplementationType == typeof(ConfiguredPipelineBehavior<,>));
        services.ShouldContain(d => d.ImplementationType == typeof(ConfiguredPreProcessor<>));
        services.ShouldContain(d => d.ImplementationType == typeof(ConfiguredPostProcessor<,>));
    }

    [Fact]
    public void AddSimpleMediator_ThrowsWhenServicesNull()
    {
        Should.Throw<ArgumentNullException>(() => ServiceCollectionExtensions.AddSimpleMediator(null!, typeof(PingCommand).Assembly));
    }

    [Fact]
    public void AddSimpleMediator_DoesNotInvokeConfigurationWhenServicesNull()
    {
        var invoked = false;

        Should.Throw<ArgumentNullException>(() =>
            ServiceCollectionExtensions.AddSimpleMediator(null!, _ => invoked = true, typeof(PingCommand).Assembly));

        invoked.ShouldBeFalse();
    }

    [Fact]
    public void AddApplicationMessaging_IsAliasOfAddSimpleMediator()
    {
        var services = new ServiceCollection();
        var result = services.AddApplicationMessaging(typeof(PingCommand).Assembly);

        result.ShouldBeSameAs(services);
        services.ShouldContain(d => d.ServiceType == typeof(IMediator));
    }

    [Fact]
    public void AddSimpleMediator_AvoidsDuplicateMediatorRegistrations()
    {
        var services = new ServiceCollection();
        services.AddSimpleMediator(typeof(PingCommand).Assembly);
        services.AddSimpleMediator(typeof(PingCommand).Assembly);

        services.Count(d => d.ServiceType == typeof(IMediator)).ShouldBe(1);
    }

    [Fact]
    public void AddSimpleMediator_RegistersMetricsAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddSimpleMediator(typeof(PingCommand).Assembly);

        services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(IMediatorMetrics)
            && descriptor.ImplementationType == typeof(MediatorMetrics)
            && descriptor.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddSimpleMediator_RegistersDiscoveredRequestPreProcessors()
    {
        var services = new ServiceCollection();
        services.AddSimpleMediator(typeof(PingCommand).Assembly);

        using var provider = services.BuildServiceProvider();
        var preProcessors = provider.GetServices<IRequestPreProcessor<PingCommand>>().ToList();

        preProcessors.ShouldContain(processor => processor.GetType() == typeof(SamplePreProcessor));
    }

    [Fact]
    public void AddSimpleMediator_RegistersDiscoveredRequestPostProcessors()
    {
        var services = new ServiceCollection();
        services.AddSimpleMediator(typeof(PingCommand).Assembly);

        using var provider = services.BuildServiceProvider();
        var postProcessors = provider.GetServices<IRequestPostProcessor<PingCommand, string>>().ToList();

        postProcessors.ShouldContain(processor => processor.GetType() == typeof(SamplePostProcessor));
    }

    [Fact]
    public async Task AddSimpleMediator_UsesLibraryAssemblyWhenNoneProvided()
    {
        var services = new ServiceCollection();
        services.AddSimpleMediator();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        mediator.ShouldNotBeNull();

        var detectorA = provider.GetRequiredService<IFunctionalFailureDetector>();
        var detectorB = provider.GetRequiredService<IFunctionalFailureDetector>();
        ReferenceEquals(detectorA, detectorB).ShouldBeTrue();
    }

    [Fact]
    public void AddSimpleMediator_RegistersBuiltInPipelineBehaviorsWhenAssembliesMissing()
    {
        var services = new ServiceCollection();
        services.AddSimpleMediator();

        services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(IPipelineBehavior<,>)
            && descriptor.ImplementationType == typeof(CommandMetricsPipelineBehavior<,>));

        services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(IPipelineBehavior<,>)
            && descriptor.ImplementationType == typeof(QueryMetricsPipelineBehavior<,>));
    }

    private sealed class ConfiguredPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public Task<Either<Error, TResponse>> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
            => next();
    }

    private sealed class ConfiguredPreProcessor<TRequest> : IRequestPreProcessor<TRequest>
    {
        public Task Process(TRequest request, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class ConfiguredPostProcessor<TRequest, TResponse> : IRequestPostProcessor<TRequest, TResponse>
    {
        public Task Process(TRequest request, Either<Error, TResponse> response, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
