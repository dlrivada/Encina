using System.Runtime.CompilerServices;
using Encina.Testing;
using Encina.Testing.Identity;
using Microsoft.Extensions.Logging.Testing;

namespace Encina.UnitTests.Core.Identity;

/// <summary>
/// The explicit-context rule of <c>AmbientRequestContext.Resolve</c> through <see cref="IEncina"/>:
/// a dispatch (or a direct set of <see cref="RequestContextAccessor.RequestContext"/>) cannot run a
/// user's request under another authenticated identity, every identity swap is logged (EventId 165)
/// with kinds only, and an explicit context is checked and dispatched as one snapshot.
/// </summary>
public sealed class ExplicitContextConflictTests
{
    private const string Anonymous = "(anonymous)";

    public sealed record Probe : IRequest<string?>;

    public sealed record MetaProbe(Action MutateBeforeRead) : IRequest<string?>;

    public sealed class MetaProbeHandler(IRequestContextAccessor accessor) : IRequestHandler<MetaProbe, string?>
    {
        public Task<Either<EncinaError, string?>> Handle(MetaProbe request, CancellationToken cancellationToken)
        {
            request.MutateBeforeRead();
            return Task.FromResult<Either<EncinaError, string?>>(accessor.RequestContext?.Metadata["k"] as string);
        }
    }

    public sealed record Ping : INotification;

    public sealed record Count : IStreamRequest<int>;

    public sealed class ProbeHandler(IRequestContextAccessor accessor) : IRequestHandler<Probe, string?>
    {
        public Task<Either<EncinaError, string?>> Handle(Probe request, CancellationToken cancellationToken)
            => Task.FromResult<Either<EncinaError, string?>>(accessor.RequestContext?.UserId ?? Anonymous);
    }

    public sealed class PingHandler : INotificationHandler<Ping>
    {
        public Task<Either<EncinaError, Unit>> Handle(Ping notification, CancellationToken cancellationToken)
            => Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }

    public sealed class CountHandler : IStreamRequestHandler<Count, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(Count request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            yield return 1;
        }
    }

    private readonly FakeLogger<global::Encina.Encina> _logger = new();

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddSingleton<ILogger<global::Encina.Encina>>(_logger);
        services.AddScoped<IRequestHandler<Probe, string?>, ProbeHandler>();
        services.AddScoped<IRequestHandler<MetaProbe, string?>, MetaProbeHandler>();
        services.AddScoped<INotificationHandler<Ping>, PingHandler>();
        services.AddScoped<IStreamRequestHandler<Count, int>, CountHandler>();
        return services.BuildServiceProvider();
    }

    private static IRequestContext User(string id) => TestRequestContext.For(TestIdentity.User(id));

    [Fact]
    public async Task Send_OverAnAmbientUser_WithAnotherUser_IsRefused_AndLogs165WithKindsOnly()
    {
        await using var provider = BuildProvider();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = User("alice-sentinel");

        var result = await provider.GetRequiredService<IEncina>().Send(new Probe(), User("mallory-sentinel"));

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetEncinaCode().ShouldBe(RequestIdentityErrorCodes.ScopeConflict));
        var record = _logger.Collector.GetSnapshot().Single(r => r.Id.Id == 165);
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("refused");
        record.Message.ShouldNotContain("sentinel");
    }

    [Fact]
    public async Task Publish_And_Stream_OverAnAmbientUser_WithAnotherUser_AreRefused()
    {
        await using var provider = BuildProvider();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = User("alice");
        var encina = provider.GetRequiredService<IEncina>();

        (await encina.Publish(new Ping(), User("bob"))).IsLeft.ShouldBeTrue();
        var items = new List<Either<EncinaError, int>>();
        await foreach (var item in encina.Stream(new Count(), User("bob")))
        {
            items.Add(item);
        }

        items.ShouldHaveSingleItem().IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Send_WithTheAmbientIdentity_IsAccepted_WithoutALog()
    {
        await using var provider = BuildProvider();
        var ambient = User("alice");
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = ambient;

        var result = await provider.GetRequiredService<IEncina>().Send(new Probe(), TestRequestContext.WithIdentity(RequestContext.CreateForTest(), ambient.Identity));

        result.ShouldBeSuccess().ShouldBe("alice");
        _logger.Collector.GetSnapshot().ShouldNotContain(r => r.Id.Id == 165);
    }

    [Fact]
    public async Task Send_WithAnExplicitAnonymousContext_OverAnAmbientUser_IsAccepted()
    {
        await using var provider = BuildProvider();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = User("alice");

        var result = await provider.GetRequiredService<IEncina>().Send(new Probe(), RequestContext.CreateForTest());

        result.ShouldBeSuccess().ShouldBe(Anonymous);
        _logger.Collector.GetSnapshot().ShouldNotContain(r => r.Id.Id == 165);
    }

    [Fact]
    public async Task Send_WithAUserContext_AndNoAmbientUser_IsAccepted_AndLogs165()
    {
        await using var provider = BuildProvider();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = null;

        var result = await provider.GetRequiredService<IEncina>().Send(new Probe(), User("job-owner"));

        result.ShouldBeSuccess().ShouldBe("job-owner");
        _logger.Collector.GetSnapshot().Single(r => r.Id.Id == 165).Message.ShouldContain("accepted");
    }

    [Fact]
    public async Task Send_AForeignExplicitContextWithANullIdentity_IsTreatedAsAnonymous()
    {
        await using var provider = BuildProvider();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = User("alice");
        var foreign = Substitute.For<IRequestContext>();
        foreign.CorrelationId.Returns("corr-foreign");
        foreign.Metadata.Returns(new Dictionary<string, object?>());

        var result = await provider.GetRequiredService<IEncina>().Send(new Probe(), foreign);

        result.ShouldBeSuccess().ShouldBe(Anonymous);
    }

    [Fact]
    public async Task Send_AForeignExplicitContext_IsCheckedAndDispatchedAsOneSnapshot()
    {
        await using var provider = BuildProvider();
        var alice = User("alice");
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = alice;

        // The first read (the check) sees alice; every later read would see mallory.
        var foreign = Substitute.For<IRequestContext>();
        foreign.CorrelationId.Returns("corr-foreign");
        foreign.Metadata.Returns(new Dictionary<string, object?>());
        foreign.Identity.Returns(alice.Identity, TestIdentity.User("mallory"));

        var result = await provider.GetRequiredService<IEncina>().Send(new Probe(), foreign);

        result.ShouldBeSuccess().ShouldBe("alice");
        _ = foreign.Received(1).Identity;
    }

    [Fact]
    public async Task Send_WithTheAmbientUserIdButOtherRoles_IsRefused()
    {
        await using var provider = BuildProvider();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = TestRequestContext.For(TestIdentity.User("alice", roles: ["reader"]));

        var result = await provider.GetRequiredService<IEncina>().Send(
            new Probe(), TestRequestContext.For(TestIdentity.User("alice", roles: ["reader", "admin"])));

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetEncinaCode().ShouldBe(RequestIdentityErrorCodes.ScopeConflict));
    }

    [Fact]
    public async Task Setter_ReplacingAnAmbientUserWithAnotherUser_Throws_AndLogs165WithKindsOnly()
    {
        await Task.Yield();
        var logger = new FakeLogger<RequestContextAccessor>();
        var accessor = new RequestContextAccessor(logger);
        var alice = User("alice-sentinel");
        accessor.RequestContext = alice;
        logger.Collector.Clear();

        Should.Throw<InvalidOperationException>(() => accessor.RequestContext = User("mallory-sentinel"));

        accessor.RequestContext.ShouldBeSameAs(alice);
        var record = logger.Collector.GetSnapshot().Single(r => r.Id.Id == 165);
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("refused");
        record.Message.ShouldNotContain("sentinel");
    }

    [Fact]
    public async Task Setter_ReplacingAnAmbientUserWithOtherRoles_Throws()
    {
        await Task.Yield();
        var accessor = new RequestContextAccessor();
        accessor.RequestContext = TestRequestContext.For(TestIdentity.User("alice"));

        Should.Throw<InvalidOperationException>(
            () => accessor.RequestContext = TestRequestContext.For(TestIdentity.User("alice", roles: ["admin"])));
    }

    [Fact]
    public async Task Setter_WithTheSameIdentity_OrAnAnonymousContext_IsAllowed_WithoutALog()
    {
        await Task.Yield();
        var logger = new FakeLogger<RequestContextAccessor>();
        var accessor = new RequestContextAccessor(logger);
        accessor.RequestContext = User("alice");
        logger.Collector.Clear();

        accessor.RequestContext = User("alice");
        accessor.RequestContext!.UserId.ShouldBe("alice");
        accessor.RequestContext = RequestContext.CreateForTest();
        accessor.RequestContext!.Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
        accessor.RequestContext = null;

        logger.Collector.GetSnapshot().ShouldNotContain(r => r.Id.Id == 165);
    }

    [Fact]
    public async Task Setter_AUserOverAnAnonymousAmbient_IsAllowed_AndLogs165()
    {
        await Task.Yield();
        var logger = new FakeLogger<RequestContextAccessor>();
        var accessor = new RequestContextAccessor(logger);
        accessor.RequestContext = RequestContext.CreateForTest();

        accessor.RequestContext = User("job-owner");

        accessor.RequestContext!.UserId.ShouldBe("job-owner");
        logger.Collector.GetSnapshot().Single(r => r.Id.Id == 165).Message.ShouldContain("accepted");
    }

    [Fact]
    public async Task Dispatch_UnderAnAnonymousAmbient_WithAnExplicitUser_RestoresTheAmbient()
    {
        await using var provider = BuildProvider();
        var accessor = provider.GetRequiredService<IRequestContextAccessor>();
        var ambient = RequestContext.CreateForTest();
        accessor.RequestContext = ambient;

        (await provider.GetRequiredService<IEncina>().Send(new Probe(), User("job-owner"))).ShouldBeSuccess().ShouldBe("job-owner");

        accessor.RequestContext.ShouldBeSameAs(ambient);
    }

    [Fact]
    public async Task Send_AForeignExplicitContext_SnapshotsItsMetadata()
    {
        await using var provider = BuildProvider();
        var metadata = new Dictionary<string, object?> { ["k"] = "original" };
        var foreign = Substitute.For<IRequestContext>();
        foreign.CorrelationId.Returns("corr-foreign");
        foreign.Metadata.Returns(metadata);
        var encina = provider.GetRequiredService<IEncina>();

        // The handler of MetaProbe reads the metadata after the caller mutated its dictionary.
        var task = encina.Send(new MetaProbe(() => metadata["k"] = "mutated"), foreign);
        var result = await task;

        result.ShouldBeSuccess().ShouldBe("original");
    }
}
