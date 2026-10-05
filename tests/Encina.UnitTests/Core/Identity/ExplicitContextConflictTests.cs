using System.Diagnostics;
using System.Runtime.CompilerServices;
using Encina.Testing;
using Encina.Testing.Identity;
using Microsoft.Extensions.Logging.Testing;

namespace Encina.UnitTests.Core.Identity;

/// <summary>
/// The explicit-context rule of <c>AmbientRequestContext.Resolve</c> through <see cref="IEncina"/>:
/// a dispatch cannot run a user's request under another authenticated identity, every identity
/// swap is logged (EventId 165) with kinds only, and the dispatch activity carries
/// <c>encina.identity.kind</c>.
/// </summary>
public sealed class ExplicitContextConflictTests
{
    private const string Anonymous = "(anonymous)";

    public sealed record Probe : IRequest<string?>;

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
    public async Task Send_AConforminglessExplicitContextWithANullIdentity_IsTreatedAsAnonymous()
    {
        await using var provider = BuildProvider();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = User("alice");
        var foreign = Substitute.For<IRequestContext>();
        foreign.CorrelationId.Returns("corr-foreign");
        foreign.Metadata.Returns(new Dictionary<string, object?>());

        var result = await provider.GetRequiredService<IEncina>().Send(new Probe(), foreign);

        result.IsRight.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false, "anonymous")]
    [InlineData(true, "user")]
    public async Task Send_TagsTheDispatchActivityWithTheIdentityKind(bool authenticated, string expected)
    {
        var tags = new System.Collections.Concurrent.ConcurrentBag<string?>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                // Only this class's request: the listener is process-wide and other tests dispatch too.
                if (activity.OperationName == "Encina.Send" && Equals(activity.GetTagItem("Encina.request_type"), typeof(Probe).FullName))
                {
                    foreach (var tag in activity.Tags)
                    {
                        tag.Value?.ShouldNotContain("sentinel-tag-user");
                    }

                    tags.Add(activity.GetTagItem("encina.identity.kind") as string);
                }
            }
        };
        ActivitySource.AddActivityListener(listener);
        await using var provider = BuildProvider();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = null;
        var context = authenticated ? User("sentinel-tag-user") : RequestContext.CreateForTest();

        (await provider.GetRequiredService<IEncina>().Send(new Probe(), context)).IsRight.ShouldBeTrue();

        tags.ShouldContain(expected);
        tags.ShouldNotContain("sentinel-tag-user");
    }

    [Theory]
    [InlineData(false, "anonymous")]
    [InlineData(true, "user")]
    public async Task Stream_TagsTheStreamActivityWithTheIdentityKind(bool authenticated, string expected)
    {
        var tags = new System.Collections.Concurrent.ConcurrentBag<string?>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.OperationName == "Encina.Stream" && Equals(activity.GetTagItem("Encina.request_type"), typeof(Count).FullName))
                {
                    tags.Add(activity.GetTagItem("encina.identity.kind") as string);
                }
            }
        };
        ActivitySource.AddActivityListener(listener);
        await using var provider = BuildProvider();
        provider.GetRequiredService<IRequestContextAccessor>().RequestContext = null;
        var context = authenticated ? User("stream-user") : RequestContext.CreateForTest();

        await foreach (var item in provider.GetRequiredService<IEncina>().Stream(new Count(), context))
        {
            item.IsRight.ShouldBeTrue();
        }

        tags.ShouldContain(expected);
    }

    [Theory]
    [InlineData(IdentityKind.Anonymous, "anonymous")]
    [InlineData(IdentityKind.User, "user")]
    [InlineData(IdentityKind.Service, "service")]
    public void ToTagValue_IsTheLowercaseKind(IdentityKind kind, string expected)
    {
        EncinaDiagnostics.ToTagValue(kind).ShouldBe(expected);
    }
}
