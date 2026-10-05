using System.Diagnostics;
using System.Runtime.CompilerServices;
using Encina.Testing.Identity;

namespace Encina.UnitTests.Core.Identity;

/// <summary>
/// The <c>encina.identity.kind</c> tag of the dispatch activities: the identity kind of the dispatch,
/// never the user id.
/// </summary>
public sealed class IdentityDiagnosticsTests
{
    public sealed record Probe : IRequest<string?>;

    public sealed record Count : IStreamRequest<int>;

    public sealed class ProbeHandler : IRequestHandler<Probe, string?>
    {
        public Task<Either<EncinaError, string?>> Handle(Probe request, CancellationToken cancellationToken)
            => Task.FromResult<Either<EncinaError, string?>>("ok");
    }

    public sealed class CountHandler : IStreamRequestHandler<Count, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(Count request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            yield return 1;
        }
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddScoped<IRequestHandler<Probe, string?>, ProbeHandler>();
        services.AddScoped<IStreamRequestHandler<Count, int>, CountHandler>();
        return services.BuildServiceProvider();
    }

    private static IRequestContext User(string id) => TestRequestContext.For(TestIdentity.User(id));

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
