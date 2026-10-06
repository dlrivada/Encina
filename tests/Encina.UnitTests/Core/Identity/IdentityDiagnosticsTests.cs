using System.Diagnostics;
using System.Runtime.CompilerServices;
using Encina.Testing;
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
        services.AddEncinaServiceIdentity(ScopeTestHost.Job);
        services.AddScoped<IRequestHandler<Probe, string?>, ProbeHandler>();
        services.AddScoped<IStreamRequestHandler<Count, int>, CountHandler>();
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Runs <paramref name="dispatch"/> with no identity, inside a user scope or inside a service
    /// scope opened through the factory (an identity is never bound by an explicit context alone).
    /// </summary>
    private static async Task RunAs(ServiceProvider provider, string expected, string userId, Func<Task> dispatch)
    {
        var scopes = provider.GetRequiredService<IRequestContextScopeFactory>();
        switch (expected)
        {
            case "user":
                (await scopes.RunAsPrincipalAsync(TestIdentity.Principal(userId), (_, _) => dispatch())).ShouldBeSuccess();
                break;
            case "service":
                (await scopes.RunAsServiceAsync(ScopeTestHost.Job, (_, _) => dispatch())).ShouldBeSuccess();
                break;
            default:
                await dispatch();
                break;
        }
    }

    [Theory]
    [InlineData("anonymous")]
    [InlineData("user")]
    [InlineData("service")]
    public async Task Send_TagsTheDispatchActivityWithTheIdentityKind(string expected)
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

        await RunAs(provider, expected, "sentinel-tag-user", async () =>
            (await provider.GetRequiredService<IEncina>().Send(new Probe())).IsRight.ShouldBeTrue());

        tags.ShouldContain(expected);
        tags.ShouldNotContain("sentinel-tag-user");
    }

    [Theory]
    [InlineData("anonymous")]
    [InlineData("user")]
    [InlineData("service")]
    public async Task Stream_TagsTheStreamActivityWithTheIdentityKind(string expected)
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

        await RunAs(provider, expected, "stream-user", async () =>
        {
            await foreach (var item in provider.GetRequiredService<IEncina>().Stream(new Count()))
            {
                item.IsRight.ShouldBeTrue();
            }
        });

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
