using Encina.Modules;

namespace Encina.UnitTests.Core;

/// <summary>
/// Proves that the core <c>AddEncina</c> registration (both overloads) produces a service graph that
/// builds with <c>ValidateOnBuild</c> and <c>ValidateScopes</c> enabled and resolves every service it
/// registers (registration completeness, AGENTS.md section 3; #1317).
/// </summary>
public sealed class AddEncinaServiceGraphTests
{
    private static readonly ServiceProviderOptions StrictOptions = new()
    {
        ValidateOnBuild = true,
        ValidateScopes = true,
    };

    private static readonly Type[] NamedServiceTypes =
    [
        typeof(IEncina),
        typeof(IRequestContextAccessor),
        typeof(IEncinaMetrics),
        typeof(IFunctionalFailureDetector),
        typeof(IModuleHandlerRegistry),
        typeof(IOptions<NotificationDispatchOptions>),
    ];

    public sealed record GraphRequest : IRequest<GraphResponse>;

    public sealed record GraphCommand : ICommand<GraphResponse>;

    public sealed record GraphQuery : IQuery<GraphResponse>;

    public sealed record GraphResponse;

    public sealed class GraphBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public ValueTask<Either<EncinaError, TResponse>> Handle(
            TRequest request,
            IRequestContext context,
            RequestHandlerCallback<TResponse> nextStep,
            CancellationToken cancellationToken) => nextStep();
    }

    public sealed class GraphPreProcessor : IRequestPreProcessor<GraphRequest>
    {
        public Task Process(GraphRequest request, IRequestContext context, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    public sealed class GraphPostProcessor : IRequestPostProcessor<GraphRequest, GraphResponse>
    {
        public Task Process(GraphRequest request, IRequestContext context, Either<EncinaError, GraphResponse> response, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    [Fact]
    public void AddEncina_WithValidateOnBuildAndScopes_ResolvesEveryRegisteredService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var added = Capture(services, s => s.AddEncina(typeof(IEncina).Assembly));

        // Assert
        added.ShouldNotBeEmpty();
        AssertGraphResolves(services, added);
    }

    [Fact]
    public void AddEncina_WithConfigureOverload_ResolvesConfiguredBehaviorAndProcessors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var added = Capture(services, s => s.AddEncina(
            configuration => configuration
                .UseParallelNotificationDispatch(NotificationDispatchStrategy.ParallelWhenAll, maxDegreeOfParallelism: 3)
                .AddPipelineBehavior(typeof(GraphBehavior<,>))
                .AddRequestPreProcessor<GraphPreProcessor>()
                .AddRequestPostProcessor<GraphPostProcessor>(),
            typeof(IEncina).Assembly));

        // Assert
        AssertGraphResolves(services, added);

        using var provider = services.BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetServices<IPipelineBehavior<GraphRequest, GraphResponse>>()
            .ShouldContain(b => b is GraphBehavior<GraphRequest, GraphResponse>);
        scope.ServiceProvider.GetServices<IRequestPreProcessor<GraphRequest>>()
            .ShouldContain(p => p is GraphPreProcessor);
        scope.ServiceProvider.GetServices<IRequestPostProcessor<GraphRequest, GraphResponse>>()
            .ShouldContain(p => p is GraphPostProcessor);

        var dispatch = scope.ServiceProvider.GetRequiredService<IOptions<NotificationDispatchOptions>>().Value;
        dispatch.Strategy.ShouldBe(NotificationDispatchStrategy.ParallelWhenAll);
        dispatch.MaxDegreeOfParallelism.ShouldBe(3);
    }

    [Fact]
    public void AddEncina_WhenARegistrationIsMissing_TheStrictGraphCheckFails()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var added = Capture(services, s => s.AddEncina(typeof(IEncina).Assembly));
        var accessor = services.Single(d => d.ServiceType == typeof(IRequestContextAccessor));
        services.Remove(accessor);

        // Act
        var act = () => AssertGraphResolves(services, added);

        // Assert
        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain(nameof(IRequestContextAccessor));
    }

    private static List<ServiceDescriptor> Capture(IServiceCollection services, Action<IServiceCollection> register)
    {
        var before = services.ToHashSet();
        register(services);
        return services.Where(d => !before.Contains(d)).ToList();
    }

    private static void AssertGraphResolves(IServiceCollection services, IReadOnlyList<ServiceDescriptor> added)
    {
        using var provider = services.BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        var serviceTypes = added
            .Select(d => d.ServiceType)
            .Concat(NamedServiceTypes)
            .Distinct();

        foreach (var serviceType in serviceTypes)
        {
            if (NamedServiceTypes.Contains(serviceType))
            {
                scope.ServiceProvider.GetRequiredService(serviceType).ShouldNotBeNull();
            }
            else if (serviceType.IsGenericTypeDefinition)
            {
                // An open registration may legitimately yield nothing for a given request kind
                // (generic constraints of the implementation); resolving must still not throw.
                foreach (var closed in CloseOverRequestKinds(serviceType))
                {
                    Should.NotThrow(() => scope.ServiceProvider.GetServices(closed).ToList());
                }
            }
            else
            {
                scope.ServiceProvider.GetServices(serviceType).ShouldNotBeEmpty($"{serviceType} resolved to no implementation");
            }
        }

        // Encina's own open-generic behaviors only apply to commands and queries: prove they are
        // instantiated, so a missing dependency of theirs cannot hide behind an empty result.
        scope.ServiceProvider.GetServices<IPipelineBehavior<GraphCommand, GraphResponse>>()
            .ShouldNotBeEmpty("no pipeline behavior resolved for a command");
        scope.ServiceProvider.GetServices<IPipelineBehavior<GraphQuery, GraphResponse>>()
            .ShouldNotBeEmpty("no pipeline behavior resolved for a query");
    }

    private static IEnumerable<Type> CloseOverRequestKinds(Type openGeneric)
    {
        if (openGeneric.GetGenericArguments().Length != 2)
        {
            yield break;
        }

        foreach (var request in new[] { typeof(GraphRequest), typeof(GraphCommand), typeof(GraphQuery) })
        {
            Type? closed;
            try
            {
                closed = openGeneric.MakeGenericType(request, typeof(GraphResponse));
            }
            catch (ArgumentException)
            {
                // The service type's own constraints exclude this request kind.
                continue;
            }

            yield return closed;
        }
    }
}
