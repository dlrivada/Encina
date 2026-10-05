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
    public void ParseThresholdMb_FallsBackToDefaultForInvalidValues(string? value, long expected)
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
        StrykerServerRecycler.ShouldRecycle(session, privateMb * OneMb, thresholdMb: 100)
            .ShouldBe(expected);
    }

    [Fact]
    public void ShouldRecycle_AtExactlyThreshold_DoesNotRecycle()
    {
        StrykerServerRecycler.ShouldRecycle(2, 100 * OneMb, thresholdMb: 100).ShouldBeFalse();
    }

    [Fact]
    public async Task OnTestSessionStarting_FirstSessionAboveThreshold_DoesNotTerminate()
    {
        var (recycler, error, terminations) = Create(privateMb: 500, thresholdMb: 100);

        await recycler.OnTestSessionStartingAsync(new SessionUid("s1"), CancellationToken.None);

        terminations().ShouldBe(0);
        error.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task OnTestSessionStarting_LaterSessionAboveThreshold_WritesOneLineAndTerminates()
    {
        var (recycler, error, terminations) = Create(privateMb: 500, thresholdMb: 100);

        await recycler.OnTestSessionStartingAsync(new SessionUid("s1"), CancellationToken.None);
        await recycler.OnTestSessionStartingAsync(new SessionUid("s2"), CancellationToken.None);

        terminations().ShouldBe(1);
        var lines = error.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines.Length.ShouldBe(1);
        lines[0].ShouldStartWith("[encina-mtp-recycle]");
        lines[0].ShouldContain("500 MB");
        lines[0].ShouldContain("100 MB");
        lines[0].ShouldContain("session 2");
    }

    [Fact]
    public async Task OnTestSessionStarting_LaterSessionBelowThreshold_DoesNotTerminate()
    {
        var (recycler, error, terminations) = Create(privateMb: 50, thresholdMb: 100);

        await recycler.OnTestSessionStartingAsync(new SessionUid("s1"), CancellationToken.None);
        await recycler.OnTestSessionStartingAsync(new SessionUid("s2"), CancellationToken.None);

        terminations().ShouldBe(0);
        error.ToString().ShouldBeEmpty();
    }

    [Fact]
    public async Task SessionCount_IsSharedAcrossHandlerInstances()
    {
        var counter = new StrykerServerRecycler.SessionCounter();
        var terminations = 0;
        StrykerServerRecycler NewHandler() =>
            new(100, counter, () => 500 * OneMb, TextWriter.Null, () => terminations++);

        await NewHandler().OnTestSessionStartingAsync(new SessionUid("s1"), CancellationToken.None);
        await NewHandler().OnTestSessionStartingAsync(new SessionUid("s2"), CancellationToken.None);

        terminations.ShouldBe(1);
    }

    [Fact]
    public async Task OnTestSessionFinishing_DoesNothing()
    {
        var (recycler, error, terminations) = Create(privateMb: 500, thresholdMb: 100);

        await recycler.OnTestSessionFinishingAsync(new SessionUid("s1"), CancellationToken.None);

        terminations().ShouldBe(0);
        error.ToString().ShouldBeEmpty();
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

    [Fact]
    public void Constructor_RejectsNonPositiveThreshold()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new StrykerServerRecycler(0, new StrykerServerRecycler.SessionCounter(), () => 0, TextWriter.Null, () => { }));
    }

    private static (StrykerServerRecycler Recycler, StringWriter Error, Func<int> Terminations) Create(
        long privateMb,
        long thresholdMb)
    {
        var error = new StringWriter();
        var terminations = 0;
        var recycler = new StrykerServerRecycler(
            thresholdMb,
            new StrykerServerRecycler.SessionCounter(),
            () => privateMb * OneMb,
            error,
            () => terminations++);
        return (recycler, error, () => terminations);
    }
}
