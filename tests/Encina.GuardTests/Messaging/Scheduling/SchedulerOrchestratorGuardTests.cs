using Encina;
using Encina.Messaging.Scheduling;
using LanguageExt;
using Shouldly;

namespace Encina.GuardTests.Messaging.Scheduling;

/// <summary>
/// Guard clause tests for SchedulerOrchestrator constructor and method parameters.
/// </summary>
public class SchedulerOrchestratorGuardTests
{
    private readonly IScheduledMessageStore _store = Substitute.For<IScheduledMessageStore>();
    private readonly SchedulingOptions _options = new();
    private readonly ILogger<SchedulerOrchestrator> _logger = NullLogger<SchedulerOrchestrator>.Instance;
    private readonly IScheduledMessageFactory _messageFactory = Substitute.For<IScheduledMessageFactory>();
    private readonly IScheduledMessageRetryPolicy _retryPolicy;
    private readonly ICronParser _cronParser = Substitute.For<ICronParser>();

    public SchedulerOrchestratorGuardTests()
    {
        _retryPolicy = new ExponentialBackoffRetryPolicy(_options);
    }

    private SchedulerOrchestrator CreateSut(ICronParser? cronParser = null) =>
        new(_store, _options, _logger, _messageFactory, _retryPolicy, cronParser);

    #region Constructor Guards

    [Fact]
    public void Constructor_NullStore_ThrowsArgumentNullException()
    {
        var act = () => new SchedulerOrchestrator(null!, _options, _logger, _messageFactory, _retryPolicy);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("store");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new SchedulerOrchestrator(_store, null!, _logger, _messageFactory, _retryPolicy);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new SchedulerOrchestrator(_store, _options, null!, _messageFactory, _retryPolicy);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullMessageFactory_ThrowsArgumentNullException()
    {
        var act = () => new SchedulerOrchestrator(_store, _options, _logger, null!, _retryPolicy);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("messageFactory");
    }

    [Fact]
    public void Constructor_NullRetryPolicy_ThrowsArgumentNullException()
    {
        var act = () => new SchedulerOrchestrator(_store, _options, _logger, _messageFactory, null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("retryPolicy");
    }

    [Fact]
    public void Constructor_NullCronParser_Succeeds()
    {
        var act = () => new SchedulerOrchestrator(_store, _options, _logger, _messageFactory, _retryPolicy, cronParser: null);
        Should.NotThrow(act);
    }

    [Fact]
    public void Constructor_NullTimeProvider_UsesSystemDefault()
    {
        var act = () => new SchedulerOrchestrator(_store, _options, _logger, _messageFactory, _retryPolicy, _cronParser, timeProvider: null);
        Should.NotThrow(act);
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        var sut = CreateSut(_cronParser);
        sut.ShouldNotBeNull();
    }

    #endregion

    #region ScheduleAsync (DateTime) Guards

    [Fact]
    public async Task ScheduleAsync_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.ScheduleAsync<TestScheduledRequest>(null!, DateTime.UtcNow.AddHours(1));
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task ScheduleAsync_PastDateTime_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.ScheduleAsync(new TestScheduledRequest(), DateTime.UtcNow.AddHours(-1));

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ScheduleAsync_FutureDateTime_ValidRequest_CallsStore()
    {
        var futureTime = DateTime.UtcNow.AddHours(1);
        var scheduledMessage = Substitute.For<IScheduledMessage>();
        scheduledMessage.Id.Returns(Guid.NewGuid());

        _messageFactory.Create(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<bool>(), Arg.Any<string?>())
            .Returns(scheduledMessage);
        _store.AddAsync(Arg.Any<IScheduledMessage>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, LanguageExt.Unit>.Right(LanguageExt.Unit.Default));

        var sut = CreateSut();
        var result = await sut.ScheduleAsync(new TestScheduledRequest(), futureTime);

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region ScheduleAsync (TimeSpan) Guards

    [Fact]
    public async Task ScheduleAsync_ZeroDelay_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.ScheduleAsync(new TestScheduledRequest(), TimeSpan.Zero);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ScheduleAsync_NegativeDelay_ReturnsError()
    {
        var sut = CreateSut();
        var result = await sut.ScheduleAsync(new TestScheduledRequest(), TimeSpan.FromSeconds(-5));

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region ScheduleRecurringAsync Guards

    [Fact]
    public async Task ScheduleRecurringAsync_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateSut(_cronParser);
        var act = () => sut.ScheduleRecurringAsync<TestScheduledRequest>(null!, "* * * * *");
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task ScheduleRecurringAsync_NullCronExpression_ThrowsArgumentException()
    {
        var sut = CreateSut(_cronParser);
        var act = () => sut.ScheduleRecurringAsync(new TestScheduledRequest(), null!);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("cronExpression");
    }

    [Fact]
    public async Task ScheduleRecurringAsync_EmptyCronExpression_ThrowsArgumentException()
    {
        var sut = CreateSut(_cronParser);
        var act = () => sut.ScheduleRecurringAsync(new TestScheduledRequest(), string.Empty);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("cronExpression");
    }

    [Fact]
    public async Task ScheduleRecurringAsync_WhitespaceCronExpression_ThrowsArgumentException()
    {
        var sut = CreateSut(_cronParser);
        var act = () => sut.ScheduleRecurringAsync(new TestScheduledRequest(), "   ");
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("cronExpression");
    }

    [Fact]
    public async Task ScheduleRecurringAsync_RecurringDisabled_ReturnsError()
    {
        var options = new SchedulingOptions { EnableRecurringMessages = false };
        var sut = new SchedulerOrchestrator(_store, options, _logger, _messageFactory, _retryPolicy, _cronParser);

        var result = await sut.ScheduleRecurringAsync(new TestScheduledRequest(), "* * * * *");

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ScheduleRecurringAsync_NoCronParser_ReturnsError()
    {
        var sut = CreateSut(cronParser: null); // No cron parser

        var result = await sut.ScheduleRecurringAsync(new TestScheduledRequest(), "* * * * *");

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region ProcessDueMessagesAsync Guards

    [Fact]
    public async Task ProcessDueMessagesAsync_NullCallback_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.ProcessDueMessagesAsync(null!);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("executeCallback");
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_StoreGetFails_ReturnsError()
    {
        var expectedError = EncinaError.New("store failure");
        _store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<IScheduledMessage>>.Left(expectedError));

        var sut = CreateSut();
        var result = await sut.ProcessDueMessagesAsync((_, _, _, _) => new ValueTask<Either<EncinaError, LanguageExt.Unit>>(Either<EncinaError, LanguageExt.Unit>.Right(LanguageExt.Unit.Default)));

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_EmptyBatch_ReturnsZero()
    {
        _store.GetDueMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IEnumerable<IScheduledMessage>>.Right(
                Enumerable.Empty<IScheduledMessage>()));

        var sut = CreateSut();
        var result = await sut.ProcessDueMessagesAsync((_, _, _, _) => new ValueTask<Either<EncinaError, LanguageExt.Unit>>(Either<EncinaError, LanguageExt.Unit>.Right(LanguageExt.Unit.Default)));

        result.IsRight.ShouldBeTrue();
        result.Match(Right: count => count, Left: _ => -1).ShouldBe(0);
    }

    #endregion

    private sealed class TestScheduledRequest
    {
        public string? Value { get; set; }
    }
}
