using Encina.Sharding;
using Encina.Sharding.Shadow;
using Encina.Sharding.Shadow.Behaviors;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Core.Sharding.Shadow;

/// <summary>
/// Proves the shadow sharding behaviors are part of the resolved pipeline (the
/// <see cref="IPipelineBehavior{TRequest, TResponse}"/> service type that the pipeline builder
/// resolves) when shadow sharding is enabled, and that they actually fire through
/// <see cref="IEncina"/>.
/// </summary>
public sealed class ShadowShardingPipelineRegistrationTests
{
    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public void Enabled_CommandPipelineContainsWriteBehaviorOnly()
    {
        using var provider = BuildProvider(shadowEnabled: true, out _);
        using var scope = provider.CreateScope();

        var types = scope.ServiceProvider.GetServices<IPipelineBehavior<ShadowCommand, string>>()
            .Select(b => b.GetType()).ToList();

        types.ShouldContain(typeof(ShadowWritePipelineBehavior<ShadowCommand, string>));
        types.ShouldNotContain(t => IsOpenGeneric(t, typeof(ShadowReadPipelineBehavior<,>)));
    }

    [Fact]
    public void Enabled_QueryPipelineContainsReadBehaviorOnly()
    {
        using var provider = BuildProvider(shadowEnabled: true, out _);
        using var scope = provider.CreateScope();

        var types = scope.ServiceProvider.GetServices<IPipelineBehavior<ShadowQuery, string>>()
            .Select(b => b.GetType()).ToList();

        types.ShouldContain(typeof(ShadowReadPipelineBehavior<ShadowQuery, string>));
        types.ShouldNotContain(t => IsOpenGeneric(t, typeof(ShadowWritePipelineBehavior<,>)));
    }

    [Fact]
    public void Enabled_EachBehaviorIsRegisteredExactlyOnce()
    {
        using var provider = BuildProvider(shadowEnabled: true, out _);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetServices<IPipelineBehavior<ShadowCommand, string>>()
            .Count(b => b is ShadowWritePipelineBehavior<ShadowCommand, string>).ShouldBe(1);
        scope.ServiceProvider.GetServices<IPipelineBehavior<ShadowQuery, string>>()
            .Count(b => b is ShadowReadPipelineBehavior<ShadowQuery, string>).ShouldBe(1);
    }

    [Fact]
    public void Disabled_NeitherBehaviorIsInThePipeline()
    {
        using var provider = BuildProvider(shadowEnabled: false, out _);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetServices<IPipelineBehavior<ShadowCommand, string>>()
            .ShouldNotContain(b => b is ShadowWritePipelineBehavior<ShadowCommand, string>);
        scope.ServiceProvider.GetServices<IPipelineBehavior<ShadowQuery, string>>()
            .ShouldNotContain(b => b is ShadowReadPipelineBehavior<ShadowQuery, string>);
    }

    [Fact]
    public async Task Enabled_SendingACommandTriggersTheShadowWrite()
    {
        using var provider = BuildProvider(shadowEnabled: true, out var shadowRouted);
        using var scope = provider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

        var result = await encina.Send(new ShadowCommand("order-1"), TestContext.Current.CancellationToken);

        result.IsRight.ShouldBeTrue();
        (await shadowRouted.Task.WaitAsync(SignalTimeout, TestContext.Current.CancellationToken)).ShouldBeTrue();
    }

    [Fact]
    public async Task Enabled_SendingAQueryTriggersTheShadowRead()
    {
        using var provider = BuildProvider(shadowEnabled: true, out var shadowRouted);
        using var scope = provider.CreateScope();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

        var result = await encina.Send(new ShadowQuery("order-1"), TestContext.Current.CancellationToken);

        result.IsRight.ShouldBeTrue();
        (await shadowRouted.Task.WaitAsync(SignalTimeout, TestContext.Current.CancellationToken)).ShouldBeTrue();
    }

    [Fact]
    public void AddShadowSharding_WithoutShadowTopology_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(() =>
            ShadowShardingServiceCollectionExtensions.AddShadowSharding(services, new ShadowShardingOptions()));
    }

    [Fact]
    public void AddShadowSharding_WithoutExistingRouter_StillRegistersBothBehaviors()
    {
        var services = new ServiceCollection();
        var options = new ShadowShardingOptions
        {
            ShadowTopology = new ShardTopology([new ShardInfo("shadow-1", "shadow-conn1")]),
        };

        ShadowShardingServiceCollectionExtensions.AddShadowSharding(services, options);

        var implementations = services
            .Where(d => d.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(d => d.ImplementationType)
            .ToList();
        implementations.ShouldBe([typeof(ShadowWritePipelineBehavior<,>), typeof(ShadowReadPipelineBehavior<,>)]);
        services.ShouldNotContain(d => d.ServiceType == typeof(ICommandPipelineBehavior<,>));
        services.ShouldNotContain(d => d.ServiceType == typeof(IQueryPipelineBehavior<,>));
    }

    [Fact]
    public void AddShadowSharding_CalledTwice_DoesNotDuplicateBehaviors()
    {
        var services = new ServiceCollection();
        var options = new ShadowShardingOptions
        {
            ShadowTopology = new ShardTopology([new ShardInfo("shadow-1", "shadow-conn1")]),
        };

        ShadowShardingServiceCollectionExtensions.AddShadowSharding(services, options);
        ShadowShardingServiceCollectionExtensions.AddShadowSharding(services, options);

        services.Count(d => d.ServiceType == typeof(IPipelineBehavior<,>)).ShouldBe(2);
    }

    private static bool IsOpenGeneric(Type type, Type openGeneric) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == openGeneric;

    private static ServiceProvider BuildProvider(bool shadowEnabled, out TaskCompletionSource<bool> shadowRouted)
    {
        var signal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        shadowRouted = signal;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina();
        services.AddScoped<IRequestHandler<ShadowCommand, string>, ShadowCommandHandler>();
        services.AddScoped<IRequestHandler<ShadowQuery, string>, ShadowQueryHandler>();

        services.AddEncinaSharding<ShadowEntity>(options =>
        {
            options.UseHashRouting().AddShard("shard-1", "conn1").AddShard("shard-2", "conn2");

            if (shadowEnabled)
            {
                options.WithShadowSharding(shadow =>
                {
                    shadow.ShadowTopology = new ShardTopology(
                    [
                        new ShardInfo("shadow-1", "shadow-conn1"),
                        new ShardInfo("shadow-2", "shadow-conn2"),
                    ]);
                    shadow.DualWriteEnabled = true;
                    shadow.ShadowReadPercentage = 100;
                    shadow.ShadowRouterFactory = _ =>
                    {
                        var router = Substitute.For<IShardRouter>();
                        router.GetShardId(Arg.Any<string>()).Returns(_ =>
                        {
                            signal.TrySetResult(true);
                            return Right<EncinaError, string>("shadow-1");
                        });
                        return router;
                    };
                });
            }
        });

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }

    public sealed record ShadowEntity(string Id);

    public sealed record ShadowCommand(string Key) : ICommand<string>;

    public sealed record ShadowQuery(string Key) : IQuery<string>;

    private sealed class ShadowCommandHandler : IRequestHandler<ShadowCommand, string>
    {
        public Task<Either<EncinaError, string>> Handle(ShadowCommand request, CancellationToken cancellationToken) =>
            Task.FromResult(Right<EncinaError, string>(request.Key));
    }

    private sealed class ShadowQueryHandler : IRequestHandler<ShadowQuery, string>
    {
        public Task<Either<EncinaError, string>> Handle(ShadowQuery request, CancellationToken cancellationToken) =>
            Task.FromResult(Right<EncinaError, string>(request.Key));
    }
}
