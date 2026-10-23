using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Extensions.TestHost;
using Microsoft.Testing.Platform.TestHost;

namespace Encina.UnitTests.TestHost;

public sealed class StrykerServerRecyclerTests
{
    private const long OneMb = 1024L * 1024L;

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("/tmp/stryker-mutant-1.txt", true)]
    public void IsMutationRun_DependsOnStrykerMutantFile(string? value, bool expected)
    {
        var result = StrykerServerRecycler.IsMutationRun(
            name => name == StrykerServerRecycler.MutantFileVariable ? value : "unrelated");

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, StrykerServerRecycler.DefaultThresholdMb)]
    [InlineData("", StrykerServerRecycler.DefaultThresholdMb)]
    [InlineData("abc", StrykerServerRecycler.DefaultThresholdMb)]
    [InlineData("0", StrykerServerRecycler.DefaultThresholdMb)]
    [InlineData("-5", StrykerServerRecycler.DefaultThresholdMb)]
    [InlineData("3072", 3072L)]
    [InlineData("9000000000000", StrykerServerRecycler.MaxThresholdMb)]
    public void ParseThresholdMb_FallsBackToDefaultOrCapsInvalidValues(string? value, long expected)
    {
        StrykerServerRecycler.ParseThresholdMb(value).ShouldBe(expected);
    }

    [Theory]
    [InlineData(1, 100_000, false)]
    [InlineData(2, 100, false)]
    [InlineData(2, 101, true)]
    [InlineData(7, 99, false)]
    public void ShouldRecycle_NeverOnFirstSessionAndOnlyAboveThreshold(int session, long privateMb, bool expected)
    {
        StrykerServerRecycler.ShouldRecycle(session, privateMb * OneMb, thresholdMb: 100).ShouldBe(expected);
    }

    [Fact]
    public void ShouldRecycle_AtExactlyThreshold_DoesNotRecycle()
    {
        StrykerServerRecycler.ShouldRecycle(2, 100 * OneMb, thresholdMb: 100).ShouldBeFalse();
    }

    [Fact]
    public void ShouldRecycle_HugeThreshold_DoesNotOverflowIntoRecycling()
    {
        StrykerServerRecycler.ShouldRecycle(2, 64L * 1024 * OneMb, thresholdMb: long.MaxValue).ShouldBeFalse();
    }

    [Fact]
    public async Task OnTestSessionStarting_FirstSessionAboveThreshold_DoesNotTerminate()
    {
        var (recycler, lines, terminations) = Create(privateMb: 500, thresholdMb: 100);

        await recycler.OnTestSessionStartingAsync(new SessionUid("s1"), CancellationToken.None);

        terminations().ShouldBe(0);
        lines.ShouldBeEmpty();
    }

    [Fact]
    public async Task OnTestSessionStarting_LaterSessionAboveThreshold_ReportsOneLineAndTerminates()
    {
        var (recycler, lines, terminations) = Create(privateMb: 500, thresholdMb: 100);

        await recycler.OnTestSessionStartingAsync(new SessionUid("s1"), CancellationToken.None);
        await recycler.OnTestSessionStartingAsync(new SessionUid("s2"), CancellationToken.None);

        terminations().ShouldBe(1);
        lines.Count.ShouldBe(1);
        lines[0].ShouldStartWith("[encina-mtp-recycle]");
        lines[0].ShouldContain("500 MB");
        lines[0].ShouldContain("100 MB");
        lines[0].ShouldContain("session 2");
    }

    [Fact]
    public async Task OnTestSessionStarting_LaterSessionBelowThreshold_DoesNotTerminate()
    {
        var (recycler, lines, terminations) = Create(privateMb: 50, thresholdMb: 100);

        await recycler.OnTestSessionStartingAsync(new SessionUid("s1"), CancellationToken.None);
        await recycler.OnTestSessionStartingAsync(new SessionUid("s2"), CancellationToken.None);

        terminations().ShouldBe(0);
        lines.ShouldBeEmpty();
    }

    [Fact]
    public async Task SessionCount_IsSharedAcrossHandlerInstances()
    {
        var counter = new StrykerServerRecycler.SessionCounter();
        var terminations = 0;
        StrykerServerRecycler NewHandler() =>
            new(100, counter, () => 500 * OneMb, _ => { }, () => terminations++);

        await NewHandler().OnTestSessionStartingAsync(new SessionUid("s1"), CancellationToken.None);
        await NewHandler().OnTestSessionStartingAsync(new SessionUid("s2"), CancellationToken.None);

        terminations.ShouldBe(1);
    }

    [Fact]
    public async Task OnTestSessionFinishing_DoesNothing()
    {
        var (recycler, lines, terminations) = Create(privateMb: 500, thresholdMb: 100);

        await recycler.OnTestSessionFinishingAsync(new SessionUid("s1"), CancellationToken.None);

        terminations().ShouldBe(0);
        lines.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExtensionMetadata_IsEnabledAndDescribed()
    {
        var (recycler, _, _) = Create(privateMb: 1, thresholdMb: 100);

        (await recycler.IsEnabledAsync()).ShouldBeTrue();
        recycler.Uid.ShouldBe(nameof(StrykerServerRecycler));
        recycler.Version.ShouldNotBeNullOrWhiteSpace();
        recycler.DisplayName.ShouldNotBeNullOrWhiteSpace();
        recycler.Description.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(StrykerServerRecycler.MaxThresholdMb + 1)]
    public void Constructor_RejectsOutOfRangeThreshold(long thresholdMb)
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new StrykerServerRecycler(thresholdMb, new StrykerServerRecycler.SessionCounter(), () => 0, _ => { }, () => { }));
    }

    [Fact]
    public void BuilderHook_WithoutStrykerMutantFile_RegistersNothing()
    {
        var builder = Substitute.For<ITestApplicationBuilder>();
        var testHost = Substitute.For<ITestHostManager>();
        builder.TestHost.Returns(testHost);

        var registered = StrykerServerRecycleBuilderHook.AddExtensions(builder, _ => null);

        registered.ShouldBeFalse();
        testHost.DidNotReceiveWithAnyArgs().AddTestSessionLifetimeHandle(default(Func<IServiceProvider, ITestSessionLifetimeHandler>)!);
    }

    [Fact]
    public void BuilderHook_WithStrykerMutantFile_RegistersTheRecycler()
    {
        var builder = Substitute.For<ITestApplicationBuilder>();
        var testHost = Substitute.For<ITestHostManager>();
        builder.TestHost.Returns(testHost);
        Func<IServiceProvider, ITestSessionLifetimeHandler>? factory = null;
        testHost.AddTestSessionLifetimeHandle(Arg.Do<Func<IServiceProvider, ITestSessionLifetimeHandler>>(f => factory = f));

        var registered = StrykerServerRecycleBuilderHook.AddExtensions(
            builder,
            name => name == StrykerServerRecycler.MutantFileVariable ? "/tmp/stryker-mutant-0.txt" : null);

        registered.ShouldBeTrue();
        factory.ShouldNotBeNull();
        factory(Substitute.For<IServiceProvider>()).ShouldBeOfType<StrykerServerRecycler>();
    }

    private static (StrykerServerRecycler Recycler, List<string> Lines, Func<int> Terminations) Create(
        long privateMb,
        long thresholdMb)
    {
        var lines = new List<string>();
        var terminations = 0;
        var recycler = new StrykerServerRecycler(
            thresholdMb,
            new StrykerServerRecycler.SessionCounter(),
            () => privateMb * OneMb,
            lines.Add,
            () => terminations++);
        return (recycler, lines, () => terminations);
    }
}
