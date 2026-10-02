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

        // Act
        var added = Capture(services, s => s.AddEncina(
            configuration => configuration
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
    }

    [Fact]
    public void AddEncina_WhenARegistrationIsMissing_TheStrictGraphCheckFails()
    {
        // Arrange
        var services = new ServiceCollection();
        var added = Capture(services, s => s.AddEncina(typeof(IEncina).Assembly));
        var accessor = services.Single(d => d.ServiceType == typeof(IRequestContextAccessor));
        services.Remove(accessor);

        // Act
        var act = () => AssertGraphResolves(services, added);

        // Assert
        act.ShouldThrow<InvalidOperationException>();
    }

    private static List<ServiceDescriptor> Capture(IServiceCollection services, Action<IServiceCollection> register)
    {
        var before = services.ToHashSet();
        register(services);
        return services.Where(d => !before.Contains(d)).ToList();
    }

    private static void AssertGraphResolves(IServiceCollection services, IReadOnlyList<ServiceDescriptor> added)
    {
        StrictOptions.ValidateOnBuild.ShouldBeTrue();
        StrictOptions.ValidateScopes.ShouldBeTrue();

        using var provider = services.BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        var serviceTypes = added
            .Select(d => d.ServiceType)
            .Concat(NamedServiceTypes)
            .Distinct();

        foreach (var serviceType in serviceTypes)
        {
            var resolvable = serviceType.IsGenericTypeDefinition
                ? CloseOverGraphPair(serviceType)
                : serviceType;

            if (resolvable is null)
            {
                continue;
            }

            if (NamedServiceTypes.Contains(serviceType))
            {
                scope.ServiceProvider.GetRequiredService(serviceType).ShouldNotBeNull();
            }
            else if (serviceType.IsGenericTypeDefinition)
            {
                // An open registration may legitimately yield nothing for the test pair (generic
                // constraints of the implementation); resolving must still not throw.
                Should.NotThrow(() => scope.ServiceProvider.GetServices(resolvable).ToList());
            }
            else
            {
                scope.ServiceProvider.GetServices(resolvable).ShouldNotBeEmpty($"{resolvable} resolved to no implementation");
            }
        }
    }

    private static Type? CloseOverGraphPair(Type openGeneric)
    {
        var arguments = openGeneric.GetGenericArguments().Length switch
        {
            1 => new[] { typeof(GraphRequest) },
            2 => [typeof(GraphRequest), typeof(GraphResponse)],
            _ => null,
        };

        if (arguments is null)
        {
            return null;
        }

        try
        {
            return openGeneric.MakeGenericType(arguments);
        }
        catch (ArgumentException)
        {
            // The generic constraints exclude the test pair (a behavior for another request kind).
            return null;
        }
    }
}
