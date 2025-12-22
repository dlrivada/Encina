using Encina.Tests.Fixtures;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddEncina_RegistersHandlersAndDependencies()
    {
        var services = new ServiceCollection();
        services.AddEncina(typeof(PingCommand).Assembly);

        await using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IMediator>();

        var handler = provider.GetRequiredService<IRequestHandler<PingCommand, string>>();
        handler.ShouldBeOfType<PingCommandHandler>();

        var notificationHandlers = provider.GetServices<INotificationHandler<DomainNotification>>();
        notificationHandlers.Count().ShouldBe(2);
    }

    [Fact]
    public void AddEncina_UsesConfigurationForHandlersAndPipeline()
    {
        var services = new ServiceCollection();
        services.AddEncina(cfg => cfg
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
    public void AddEncina_ThrowsWhenServicesNull()
    {
        Should.Throw<ArgumentNullException>(() => ServiceCollectionExtensions.AddEncina(null!, typeof(PingCommand).Assembly));
    }

    [Fact]
    public void AddEncina_DoesNotInvokeConfigurationWhenServicesNull()
    {
        var invoked = false;

        Should.Throw<ArgumentNullException>(() =>
            ServiceCollectionExtensions.AddEncina(null!, _ => invoked = true, typeof(PingCommand).Assembly));

        invoked.ShouldBeFalse();
    }

    [Fact]
    public void AddApplicationMessaging_IsAliasOfAddEncina()
    {
        var services = new ServiceCollection();
        var result = services.AddApplicationMessaging(typeof(PingCommand).Assembly);

        result.ShouldBeSameAs(services);
        services.ShouldContain(d => d.ServiceType == typeof(IMediator));
    }

    [Fact]
    public void AddEncina_AvoidsDuplicateMediatorRegistrations()
    {
        var services = new ServiceCollection();
        services.AddEncina(typeof(PingCommand).Assembly);
        services.AddEncina(typeof(PingCommand).Assembly);

        services.Count(d => d.ServiceType == typeof(IMediator)).ShouldBe(1);
    }

    [Fact]
    public void AddEncina_RegistersMetricsAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddEncina(typeof(PingCommand).Assembly);

        services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(IMediatorMetrics)
            && descriptor.ImplementationType == typeof(MediatorMetrics)
            && descriptor.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncina_RegistersDiscoveredRequestPreProcessors()
    {
        var services = new ServiceCollection();
        services.AddEncina(typeof(PingCommand).Assembly);

        using var provider = services.BuildServiceProvider();
        var preProcessors = provider.GetServices<IRequestPreProcessor<PingCommand>>().ToList();

        preProcessors.ShouldContain(processor => processor.GetType() == typeof(SamplePreProcessor));
    }

    [Fact]
    public void AddEncina_RegistersDiscoveredRequestPostProcessors()
    {
        var services = new ServiceCollection();
        services.AddEncina(typeof(PingCommand).Assembly);

        using var provider = services.BuildServiceProvider();
        var postProcessors = provider.GetServices<IRequestPostProcessor<PingCommand, string>>().ToList();

        postProcessors.ShouldContain(processor => processor.GetType() == typeof(SamplePostProcessor));
    }

    [Fact]
    public async Task AddEncina_UsesLibraryAssemblyWhenNoneProvided()
    {
        var services = new ServiceCollection();
        services.AddEncina();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        mediator.ShouldNotBeNull();

        var detectorA = provider.GetRequiredService<IFunctionalFailureDetector>();
        var detectorB = provider.GetRequiredService<IFunctionalFailureDetector>();
        ReferenceEquals(detectorA, detectorB).ShouldBeTrue();
    }

    [Fact]
    public void AddEncina_RegistersBuiltInPipelineBehaviorsWhenAssembliesMissing()
    {
        var services = new ServiceCollection();
        services.AddEncina();

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
        public ValueTask<Either<MediatorError, TResponse>> Handle(TRequest request, IRequestContext context, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }

    private sealed class ConfiguredPreProcessor<TRequest> : IRequestPreProcessor<TRequest>
    {
        public Task Process(TRequest request, IRequestContext context, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class ConfiguredPostProcessor<TRequest, TResponse> : IRequestPostProcessor<TRequest, TResponse>
    {
        public Task Process(TRequest request, IRequestContext context, Either<MediatorError, TResponse> response, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
