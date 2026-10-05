using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Encina.Modules;

namespace Encina.UnitTests.Core;

/// <summary>
/// Proves that the core <c>AddEncina</c> registration (both overloads) produces a service graph that
/// builds with <c>ValidateOnBuild</c> and <c>ValidateScopes</c> enabled and resolves every service it
/// registers, and that assembly scanning discovers stream handlers and stream pipeline behaviors
/// (registration completeness, AGENTS.md section 3; #1317).
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
        typeof(TimeProvider),
        typeof(IRequestIdentityFactory),
        typeof(IOptions<RequestIdentityOptions>),
        typeof(IStartupValidator),
    ];

    // Open generics the closing helpers genuinely cannot close. Empty on purpose: add a type here
    // only with a comment that says why no request kind of this test can satisfy its constraints.
    private static readonly System.Collections.Generic.HashSet<Type> UnclosableOpenGenerics = [];

    private static readonly Lazy<Assembly> ScannedAssembly = new(BuildScannedAssembly);

    /// <summary>
    /// Every non-generic service type <c>AddEncina</c> registers exactly once, derived from the
    /// descriptors it adds so a new <c>TryAdd*</c> is covered automatically. Options configuration
    /// descriptors are excluded: <c>IOptions&lt;T&gt;</c> always resolves, and the configured values
    /// are asserted by the configure-overload test instead.
    /// </summary>
    public static TheoryData<Type> SingleInstanceServices
    {
        get
        {
            var data = new TheoryData<Type>();
            foreach (var type in DeriveSingleInstanceServices())
            {
                data.Add(type);
            }

            return data;
        }
    }

    private static List<Type> DeriveSingleInstanceServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var added = Capture(services, s => s.AddEncina(typeof(IEncina).Assembly));
        return added
            .Where(d => !d.ServiceType.IsGenericType && !d.ServiceType.IsGenericTypeDefinition)
            .GroupBy(d => d.ServiceType)
            .Where(g => g.Count() == 1)
            .Select(g => g.Key)
            .ToList();
    }

    public sealed record GraphRequest : IRequest<GraphResponse>;

    public sealed record GraphCommand : ICommand<GraphResponse>;

    public sealed record GraphQuery : IQuery<GraphResponse>;

    public sealed record GraphResponse;

    public sealed record GraphStreamRequest : IStreamRequest<GraphItem>;

    public sealed record GraphItem;

    public sealed record ScanStreamRequest : IStreamRequest<int>;

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

    /// <summary>
    /// Abstract (so the scanner ignores it) base of the stream handler the scanned dynamic assembly defines.
    /// </summary>
    public abstract class StreamHandlerBase : IStreamRequestHandler<ScanStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            ScanStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var item in new[] { 1, 2, 3 })
            {
                yield return item;
                await Task.Yield();
            }
        }
    }

    public static class StreamTrace
    {
        private static int _streamsSeen;

        public static int StreamsSeen => Volatile.Read(ref _streamsSeen);

        public static void Record() => Interlocked.Increment(ref _streamsSeen);
    }

    /// <summary>
    /// Abstract open-generic base of the stream behavior the scanned dynamic assembly defines; it counts
    /// how many streams went through it.
    /// </summary>
    public abstract class StreamBehaviorBase<TRequest, TItem> : IStreamPipelineBehavior<TRequest, TItem>
        where TRequest : IStreamRequest<TItem>
    {
        public async IAsyncEnumerable<Either<EncinaError, TItem>> Handle(
            TRequest request,
            IRequestContext context,
            StreamHandlerCallback<TItem> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            StreamTrace.Record();
            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                yield return item;
            }
        }
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
        added.ShouldContain(d => d.ServiceType == typeof(IConfigureOptions<NotificationDispatchOptions>));
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
    public async Task AddEncina_ScanningAnAssembly_DiscoversAndRunsStreamHandlerAndStreamBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var assembly = ScannedAssembly.Value;
        var handlerType = assembly.GetType("ScannedStreamHandler")!;
        var behaviorType = assembly.GetType("ScannedStreamBehavior`2")!;

        // Act
        var added = Capture(services, s => s.AddEncina(assembly, typeof(IEncina).Assembly));

        // Assert: discovery by the scanner
        added.ShouldContain(d =>
            d.ServiceType == typeof(IStreamRequestHandler<ScanStreamRequest, int>)
            && d.ImplementationType == handlerType);
        added.ShouldContain(d =>
            d.ServiceType == typeof(IStreamPipelineBehavior<,>)
            && d.ImplementationType == behaviorType);

        // Assert: the graph builds strictly and resolves them, and a stream executes through the behavior
        AssertGraphResolves(services, added);

        using var provider = services.BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetServices<IStreamPipelineBehavior<ScanStreamRequest, int>>()
            .Count(b => b.GetType().GetGenericTypeDefinition() == behaviorType)
            .ShouldBe(1);

        var seenBefore = StreamTrace.StreamsSeen;
        var items = new List<int>();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
        await foreach (var item in encina.Stream(new ScanStreamRequest(), TestContext.Current.CancellationToken))
        {
            items.Add(item.Match(Right: value => value, Left: _ => -1));
        }

        items.ShouldBe([1, 2, 3]);
        StreamTrace.StreamsSeen.ShouldBe(seenBefore + 1);
    }

    [Theory]
    [MemberData(nameof(SingleInstanceServices))]
    public void AddEncina_WhenARegistrationIsMissing_TheStrictGraphCheckFailsNamingIt(Type missing)
    {
        // Arrange: model AddEncina forgetting the registration (it is absent from the captured diff too)
        var services = new ServiceCollection();
        services.AddLogging();
        var added = Capture(services, s => s.AddEncina(typeof(IEncina).Assembly));
        var descriptor = services.Single(d => d.ServiceType == missing);
        services.Remove(descriptor);
        var addedWithout = added.Where(d => d != descriptor).ToList();

        // Act
        var act = () => AssertGraphResolves(services, addedWithout);

        // Assert
        act.ShouldThrow<Exception>().Message.ShouldContain(missing.FullName!);
    }

    [Fact]
    public void SingleInstanceServices_ContainTheKnownRegistrations()
    {
        var derived = DeriveSingleInstanceServices();

        derived.ShouldContain(typeof(IEncina));
        derived.ShouldContain(typeof(IRequestContextAccessor));
        derived.ShouldContain(typeof(IEncinaMetrics));
        derived.ShouldContain(typeof(IFunctionalFailureDetector));
        derived.ShouldContain(typeof(IModuleHandlerRegistry));
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

        var neverClosed = new List<Type>();

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
                var closedTypes = CloseOverRequestKinds(serviceType).ToList();
                if (closedTypes.Count == 0 && !UnclosableOpenGenerics.Contains(serviceType))
                {
                    neverClosed.Add(serviceType);
                }

                foreach (var closed in closedTypes)
                {
                    Should.NotThrow(() => scope.ServiceProvider.GetServices(closed).ToList());
                }
            }
            else
            {
                scope.ServiceProvider.GetServices(serviceType).ShouldNotBeEmpty($"{serviceType} resolved to no implementation");
            }
        }

        neverClosed.ShouldBeEmpty(
            "open generic registrations that no request kind of this test could close: "
            + string.Join(", ", neverClosed.Select(t => t.Name)));

        // Encina's own open-generic behaviors only apply to commands and queries: prove each kind gets
        // exactly its two built-in behaviors (activity and metrics), so one dropped from the scan or a
        // missing dependency cannot hide behind an empty or partial result.
        BuiltInBehaviorCount<GraphCommand>(scope).ShouldBe(2);
        BuiltInBehaviorCount<GraphQuery>(scope).ShouldBe(2);
    }

    private static int BuiltInBehaviorCount<TRequest>(IServiceScope scope)
        where TRequest : IRequest<GraphResponse>
        => scope.ServiceProvider.GetServices<IPipelineBehavior<TRequest, GraphResponse>>()
            .Count(b => b.GetType().Assembly == typeof(IEncina).Assembly);

    private static IEnumerable<Type> CloseOverRequestKinds(Type openGeneric)
    {
        (Type Request, Type Response)[] kinds =
        [
            (typeof(GraphRequest), typeof(GraphResponse)),
            (typeof(GraphCommand), typeof(GraphResponse)),
            (typeof(GraphQuery), typeof(GraphResponse)),
            (typeof(GraphStreamRequest), typeof(GraphItem)),
        ];

        var arity = openGeneric.GetGenericArguments().Length;
        if (arity == 1)
        {
            // One-argument services (pre-processors) take the request only.
            var closed = TryClose(openGeneric, typeof(GraphRequest));
            if (closed is not null)
            {
                yield return closed;
            }

            yield break;
        }

        if (arity != 2)
        {
            yield break;
        }

        foreach (var (request, response) in kinds)
        {
            var closed = TryClose(openGeneric, request, response);
            if (closed is not null)
            {
                yield return closed;
            }
        }
    }

    private static Type? TryClose(Type openGeneric, params Type[] arguments)
    {
        try
        {
            return openGeneric.MakeGenericType(arguments);
        }
        catch (ArgumentException)
        {
            // The service type's own constraints exclude this request kind.
            return null;
        }
    }

    /// <summary>
    /// Emits an assembly holding only the stream types the scan test needs, so scanning it does not
    /// register the hundreds of unrelated handlers of the test assembly. The types derive from abstract
    /// bases defined above, so no method body has to be emitted.
    /// </summary>
    private static AssemblyBuilder BuildScannedAssembly()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("AddEncinaStreamScanFixture"),
            AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("AddEncinaStreamScanFixture");

        var handler = module.DefineType(
            "ScannedStreamHandler",
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed,
            typeof(StreamHandlerBase));
        handler.DefineDefaultConstructor(MethodAttributes.Public);
        handler.CreateType();

        var behavior = module.DefineType(
            "ScannedStreamBehavior`2",
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);
        var parameters = behavior.DefineGenericParameters("TRequest", "TItem");
        parameters[0].SetInterfaceConstraints(typeof(IStreamRequest<>).MakeGenericType(parameters[1]));
        var baseType = typeof(StreamBehaviorBase<,>).MakeGenericType(parameters[0], parameters[1]);
        behavior.SetParent(baseType);
        var baseConstructor = TypeBuilder.GetConstructor(
            baseType,
            typeof(StreamBehaviorBase<,>).GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                Type.EmptyTypes)!);
        var constructor = behavior.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
        var il = constructor.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, baseConstructor);
        il.Emit(OpCodes.Ret);
        behavior.CreateType();

        return assembly;
    }
}
