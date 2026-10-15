#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Security.Audit;
using Encina.TestInfrastructure.Extensions;
using Encina.UnitTests.Support;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// The audit write is awaited, runs in a child scope of its own and a failed write is never
/// reported as success (#1869, AGENTS.md section 3, SPEC-002 DEC-006).
/// </summary>
public sealed class AuditPipelineBehaviorFailureModeTests
{
    private const string Sentinel = "SENTINEL-audit-store-message";

    private readonly IOperationAuditStore _store = Substitute.For<IOperationAuditStore>();
    private readonly IOperationAuditEntryFactory _factory = Substitute.For<IOperationAuditEntryFactory>();
    private readonly FakeLogger<AuditPipelineBehavior<FailureModeCommand, Unit>> _logger = new();

    public AuditPipelineBehaviorFailureModeTests()
    {
        AuditPipelineBehavior<FailureModeCommand, Unit>.ClearCache();
        _factory.Create(
                Arg.Any<object>(), Arg.Any<object?>(), Arg.Any<IRequestContext>(), Arg.Any<AuditOutcome>(),
                Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
            .Returns(_ => new OperationAuditEntry
            {
                Id = Guid.NewGuid(),
                CorrelationId = "corr",
                Action = "Test",
                EntityType = "TestEntity",
                Outcome = AuditOutcome.Success,
                TimestampUtc = DateTime.UtcNow,
                StartedAtUtc = DateTimeOffset.UtcNow,
                CompletedAtUtc = DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, object?>()
            });
    }

    private AuditPipelineBehavior<FailureModeCommand, Unit> CreateSut(
        OperationAuditFailureMode? mode, IServiceScopeFactory? scopeFactory = null)
    {
        var options = new OperationAuditOptions { AuditAllCommands = true };
        if (mode is { } value)
        {
            options.FailureMode = value;
        }

        return new(scopeFactory ?? SingletonScopeFactory.For(_store), _factory, Options.Create(options), _logger);
    }

    private static ValueTask<Either<EncinaError, Unit>> Ok() =>
        new(Right<EncinaError, Unit>(Unit.Default));

    private static Task<Either<EncinaError, Unit>> HandleAsync(
        AuditPipelineBehavior<FailureModeCommand, Unit> sut,
        RequestHandlerCallback<Unit>? next = null) =>
        sut.Handle(new FailureModeCommand(), RequestContext.CreateForTest(), next ?? Ok, CancellationToken.None).AsTask();

    private void StoreReturnsLeft() =>
        _store.RecordAsync(Arg.Any<OperationAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(
                Left<EncinaError, Unit>(EncinaErrors.Create("audit.store.sentinel", Sentinel))));

    private void StoreThrows() =>
        _store.RecordAsync(Arg.Any<OperationAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ => throw new InvalidOperationException(Sentinel));

    [Fact]
    public void FailureMode_DefaultsToFailClosed() =>
        new OperationAuditOptions().FailureMode.ShouldBe(OperationAuditFailureMode.FailClosed);

    [Fact]
    public async Task Handle_WriteIsCompletedBeforeTheBehaviorReturns()
    {
        var gate = new TaskCompletionSource();
        var completed = false;
        _store.RecordAsync(Arg.Any<OperationAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(WriteAsync()));

        async Task<Either<EncinaError, Unit>> WriteAsync()
        {
            await gate.Task;
            completed = true;
            return Right<EncinaError, Unit>(Unit.Default);
        }

        var pending = HandleAsync(CreateSut(null));
        await Task.Delay(50, TestContext.Current.CancellationToken);

        pending.IsCompleted.ShouldBeFalse("the behavior must wait for the audit write");
        completed.ShouldBeFalse();

        gate.SetResult();
        var result = await pending;

        completed.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_FailClosed_StoreLeft_FailsASuccessfulRequestWithTheAuditCode()
    {
        StoreReturnsLeft();

        var result = await HandleAsync(CreateSut(OperationAuditFailureMode.FailClosed));

        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone(string.Empty))
            .ShouldBe(OperationAuditErrors.WriteFailedCode);
        _logger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task Handle_FailClosed_ThrowingStore_FailsASuccessfulRequestWithoutLeakingTheMessage()
    {
        StoreThrows();

        var result = await HandleAsync(CreateSut(null));

        result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone(string.Empty))
            .ShouldBe(OperationAuditErrors.WriteFailedCode);
        result.Match(Right: _ => string.Empty, Left: e => e.Message).ShouldNotContain(Sentinel);
        RedactedExceptionLogAssert.LoggedOnlyRedacted(_logger, Sentinel);
    }

    [Theory]
    [InlineData(OperationAuditFailureMode.FailClosed)]
    [InlineData(OperationAuditFailureMode.BestEffort)]
    public async Task Handle_HandlerLeft_IsPreservedWhateverTheAuditWriteDoes(OperationAuditFailureMode mode)
    {
        StoreReturnsLeft();
        var handlerError = EncinaErrors.Create("handler.failed", "handler said no");

        var result = await HandleAsync(
            CreateSut(mode),
            () => new ValueTask<Either<EncinaError, Unit>>(Left<EncinaError, Unit>(handlerError)));

        result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone(string.Empty)).ShouldBe("handler.failed");
        _logger.Collector.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Warning);
    }

    [Theory]
    [InlineData(OperationAuditFailureMode.FailClosed)]
    [InlineData(OperationAuditFailureMode.BestEffort)]
    public async Task Handle_HandlerThrows_RethrowsTheOriginalExceptionEvenWhenTheAuditWriteFails(OperationAuditFailureMode mode)
    {
        StoreThrows();

        var act = () => HandleAsync(CreateSut(mode), () => throw new ArgumentException("handler boom"));

        (await Should.ThrowAsync<ArgumentException>(act)).Message.ShouldBe("handler boom");
        await _store.Received(1).RecordAsync(Arg.Any<OperationAuditEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HandlerCancelled_RethrowsAndStillAudits()
    {
        _store.RecordAsync(Arg.Any<OperationAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Unit.Default));

        var act = () => HandleAsync(CreateSut(null), () => throw new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(act);
        await _store.Received(1).RecordAsync(Arg.Any<OperationAuditEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BestEffort_StoreLeft_PassesAndLogsTheDroppedWrite()
    {
        StoreReturnsLeft();

        var result = await HandleAsync(CreateSut(OperationAuditFailureMode.BestEffort));

        result.IsRight.ShouldBeTrue();
        var warning = _logger.Collector.GetSnapshot().Single(r => r.Level == LogLevel.Warning);
        warning.Id.Id.ShouldBe(5000);
        warning.Message.ShouldContain("audit.store.sentinel");
        warning.Message.ShouldNotContain(Sentinel);
    }

    [Fact]
    public async Task Handle_BestEffort_ThrowingStore_PassesAndLogsOnlyARedactedException()
    {
        StoreThrows();

        var result = await HandleAsync(CreateSut(OperationAuditFailureMode.BestEffort));

        result.IsRight.ShouldBeTrue();
        _logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 5001 && r.Level == LogLevel.Warning);
        RedactedExceptionLogAssert.LoggedOnlyRedacted(_logger, Sentinel);
    }

    [Fact]
    public async Task Handle_FactoryThrows_IsAnAuditFailureLikeAFailedWrite()
    {
        _factory.Create(
                Arg.Any<object>(), Arg.Any<object?>(), Arg.Any<IRequestContext>(), Arg.Any<AuditOutcome>(),
                Arg.Any<string?>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
            .Returns(_ => throw new InvalidOperationException(Sentinel));

        var result = await HandleAsync(CreateSut(null));

        result.Match(Right: _ => string.Empty, Left: e => e.GetCode().IfNone(string.Empty))
            .ShouldBe(OperationAuditErrors.WriteFailedCode);
        await _store.DidNotReceiveWithAnyArgs().RecordAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WritesInAChildScopeThatIsDisposedAndNeverTouchesTheRequestScope()
    {
        var services = new ServiceCollection();
        services.AddScoped<IOperationAuditStore, ScopedStore>();
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var requestScope = provider.CreateAsyncScope();
        var requestStore = (ScopedStore)requestScope.ServiceProvider.GetRequiredService<IOperationAuditStore>();

        var sut = CreateSut(null, provider.GetRequiredService<IServiceScopeFactory>());
        var result = await HandleAsync(sut);

        result.IsRight.ShouldBeTrue();
        requestStore.Writes.ShouldBe(0, "the request scope's store is never used");
        requestStore.Disposed.ShouldBeFalse("the request scope is untouched");
        var childStore = ScopedStore.Instances.Single(s => !ReferenceEquals(s, requestStore) && s.Writes == 1);
        childStore.Writes.ShouldBe(1);
        childStore.Disposed.ShouldBeTrue("the child scope is disposed once the write completes");
    }

    public sealed record FailureModeCommand : ICommand<Unit>;

    private sealed class ScopedStore : IOperationAuditStore, IDisposable
    {
        public static List<ScopedStore> Instances { get; } = [];

        public ScopedStore()
        {
            lock (Instances)
            {
                Instances.Add(this);
            }
        }

        public int Writes { get; private set; }

        public bool Disposed { get; private set; }

        public void Dispose() => Disposed = true;

        public ValueTask<Either<EncinaError, Unit>> RecordAsync(OperationAuditEntry entry, CancellationToken cancellationToken = default)
        {
            Writes++;
            return new(Unit.Default);
        }

        public ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByEntityAsync(string entityType, string? entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByUserAsync(string userId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<Either<EncinaError, PagedResult<OperationAuditEntry>>> QueryAsync(OperationAuditQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<Either<EncinaError, int>> PurgeEntriesAsync(DateTime olderThanUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
