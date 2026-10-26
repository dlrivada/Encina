using System.Collections.Concurrent;
using System.Security.Cryptography;

using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Encina.Audit.Marten.Events;
using Encina.Audit.Marten.Projections;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;

using JasperFx;
using JasperFx.Events.Daemon;

using Marten;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.Marten.Audit;

/// <summary>
/// Issue #2770 (D14, D15, Q1): on real Marten and PostgreSQL, an audit event whose encrypted field cannot be read
/// stops the projection's shard instead of being skipped or projected as plaintext or as <c>[SHREDDED]</c>, while a key
/// destroyed with an erasure reference reads as <c>[SHREDDED]</c> and the shard keeps running.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class AuditProjectionUnreadableEventIntegrationTests(MartenFixture fixture) : IAsyncLifetime
{
    private const string OperationShard = "OperationAuditEntryProjection:All";
    private const string Plaintext = "plaintext-user-4711";
    private const string DestroyedPeriod = "2020-01";
    private const string LivePeriod = "2026-10";
    private static readonly DateTimeOffset Requested = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

    private readonly CapturingLoggerProvider _logs = new();
    private DocumentStore? _store;
    private IProjectionDaemon? _daemon;
    private ILoggerFactory? _loggerFactory;

    public async ValueTask InitializeAsync()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        _loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(_logs));
        var schema = $"audit_unreadable_{Guid.NewGuid():N}"[..40];

        _store = DocumentStore.For(options =>
        {
            options.Connection(fixture.ConnectionString);
            options.DatabaseSchemaName = schema;
            options.Events.StreamIdentity = JasperFx.Events.StreamIdentity.AsString;
            new ConfigureMartenOperationAuditProjections(
                Options.Create(new MartenOperationAuditOptions()), _loggerFactory).Configure(options);
        });

        await _store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_daemon is not null)
        {
            await _daemon.StopAllAsync();
            _daemon.Dispose();
        }

        _store?.Dispose();
        _loggerFactory?.Dispose();
    }

    [Fact]
    public async Task KeyDestroyedWithErasureReference_ProjectsShreddedAndTheShardKeepsRunning()
    {
        SkipWhenUnavailable();
        var live = await SeedKeyAsync(LivePeriod);
        var old = await SeedKeyAsync(DestroyedPeriod);
        var first = await AppendAsync(LivePeriod, live);
        var erased = await AppendAsync(DestroyedPeriod, old);
        var last = await AppendAsync(LivePeriod, live);
        await DestroyAsync(new ErasureReference("erasure-42", Requested, "dpo"));

        await StartDaemonAsync();
        await WaitForAsync(() => IsProjectedAsync(last));

        (await Load(first))!.IsShredded.ShouldBeFalse();
        var shredded = (await Load(erased))!;
        shredded.IsShredded.ShouldBeTrue();
        shredded.UserId.ShouldBe(MartenOperationAuditOptions.DefaultShreddedPlaceholder);
        (await Load(last))!.UserId.ShouldBe(Plaintext);
        _daemon!.StatusFor(OperationShard).ShouldBe(AgentStatus.Running);
        _daemon.HasAnyPaused().ShouldBeFalse();
    }

    [Fact]
    public async Task KeyDestroyedWithoutErasureReference_FailsTypedAndTheShardStops()
    {
        SkipWhenUnavailable();
        var live = await SeedKeyAsync(LivePeriod);
        var old = await SeedKeyAsync(DestroyedPeriod);
        var first = await AppendAsync(LivePeriod, live);
        var poison = await AppendAsync(DestroyedPeriod, old);
        var after = await AppendAsync(LivePeriod, live);
        await DestroyAsync(erasureReference: null);

        await StartDaemonAsync();

        await AssertShardStopsAtAsync(first, poison, after);
        AssertCapturedFailureWithoutLeaks(envelope: null);
    }

    [Fact]
    public async Task MalformedTagLength_FailsTypedAndTheShardStops()
    {
        SkipWhenUnavailable();
        var live = await SeedKeyAsync(LivePeriod);
        var first = await AppendAsync(LivePeriod, live);
        var tampered = EncryptedField.Encrypt(Plaintext, live, "temporal:2026-10:v1");
        tampered = tampered with { Value = tampered.Value!.Replace("\"t\":\"", "\"t\":\"AAAA", StringComparison.Ordinal) };
        var poison = await AppendAsync(LivePeriod, live, tampered);
        var after = await AppendAsync(LivePeriod, live);

        await StartDaemonAsync();

        await AssertShardStopsAtAsync(first, poison, after);
        AssertCapturedFailureWithoutLeaks(tampered.Value);
    }

    [Fact]
    public async Task UnmarkedValue_FailsTypedAndTheShardStops()
    {
        SkipWhenUnavailable();
        var live = await SeedKeyAsync(LivePeriod);
        var first = await AppendAsync(LivePeriod, live);
        var unmarked = new EncryptedField { Value = $"{{\"plain\":\"{Plaintext}\"}}", KeyId = "temporal:2026-10:v1" };
        var poison = await AppendAsync(LivePeriod, live, unmarked);
        var after = await AppendAsync(LivePeriod, live);

        await StartDaemonAsync();

        await AssertShardStopsAtAsync(first, poison, after);
        AssertCapturedFailureWithoutLeaks(unmarked.Value);
    }

    /// <summary>
    /// D20 evidence: an event whose period has neither an active key nor a destroyed marker throws
    /// <see cref="KeyNotFoundException"/>. The test records what the daemon does, then stores the missing key and
    /// reports whether the event ever gets projected (that is, whether the daemon retried it).
    /// </summary>
    [Fact]
    public async Task NoActiveKeyAndNoMarker_ShardPausesAndTheEventIsNotRetried()
    {
        SkipWhenUnavailable();
        const string period = "2019-05";
        var key = RandomNumberGenerator.GetBytes(32);
        var live = await SeedKeyAsync(LivePeriod);
        var first = await AppendAsync(LivePeriod, live);
        var missingKey = await AppendAsync(period, key);
        var after = await AppendAsync(LivePeriod, live);

        await StartDaemonAsync();
        await Task.Delay(TimeSpan.FromSeconds(8));

        var statusBefore = _daemon!.StatusFor(OperationShard);
        var afterProjectedBefore = await IsProjectedAsync(after);

        await using (var session = _store!.LightweightSession())
        {
            session.Store(new TemporalKeyDocument
            {
                Id = TemporalPeriodHelper.FormatKeyId(period, 1),
                Period = period,
                KeyMaterial = key,
                Version = 1,
                Status = TemporalKeyStatus.Active,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            await session.SaveChangesAsync();
        }

        var recovered = await TryWaitForAsync(() => IsProjectedAsync(missingKey), TimeSpan.FromSeconds(30));
        var statusAfter = _daemon.StatusFor(OperationShard);

        var evidence = $"D20 EVIDENCE: statusWhileKeyMissing={statusBefore}, laterEventProjectedWhileKeyMissing={afterProjectedBefore}, " +
                       $"eventProjectedAfterKeyStored={recovered}, statusAfter={statusAfter}; log: " +
                       string.Join(" || ", _logs.Entries.Where(e => !e.Contains("high water mark", StringComparison.Ordinal)).Distinct().Take(10));
        TestContext.Current.SendDiagnosticMessage(evidence);

        statusBefore.ShouldBe(AgentStatus.Paused);
        statusAfter.ShouldBe(AgentStatus.Paused, "the daemon does not retry a projection that throws KeyNotFoundException");
        recovered.ShouldBeFalse();
        afterProjectedBefore.ShouldBeFalse();
        (await IsProjectedAsync(first)).ShouldBeFalse("the failing batch commits nothing");
    }

    /// <summary>
    /// The recovery the runbook documents: after the operator repairs the cause (here, the missing key is restored),
    /// rebuilding the projection projects every event, including the one that stopped the shard.
    /// </summary>
    [Fact]
    public async Task AfterTheCauseIsRepaired_RebuildingTheProjectionProjectsEveryEvent()
    {
        SkipWhenUnavailable();
        const string period = "2019-05";
        var key = RandomNumberGenerator.GetBytes(32);
        var live = await SeedKeyAsync(LivePeriod);
        var first = await AppendAsync(LivePeriod, live);
        var blocked = await AppendAsync(period, key);
        var after = await AppendAsync(LivePeriod, live);

        await StartDaemonAsync();
        await WaitForAsync(() => Task.FromResult(_daemon!.StatusFor(OperationShard) != AgentStatus.Running), TimeSpan.FromSeconds(60));
        _daemon!.StatusFor(OperationShard).ShouldBe(AgentStatus.Paused);

        await using (var session = _store!.LightweightSession())
        {
            session.Store(new TemporalKeyDocument
            {
                Id = TemporalPeriodHelper.FormatKeyId(period, 1),
                Period = period,
                KeyMaterial = key,
                Version = 1,
                Status = TemporalKeyStatus.Active,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            await session.SaveChangesAsync();
        }

        await _daemon.RebuildProjectionAsync("OperationAuditEntryProjection", CancellationToken.None);

        (await IsProjectedAsync(first)).ShouldBeTrue();
        (await Load(blocked))!.UserId.ShouldBe(Plaintext);
        (await IsProjectedAsync(after)).ShouldBeTrue();
    }

    private async Task AssertShardStopsAtAsync(Guid first, Guid poison, Guid after)
    {
        await WaitForAsync(() => Task.FromResult(_daemon!.StatusFor(OperationShard) != AgentStatus.Running), TimeSpan.FromSeconds(60));
        await Task.Delay(TimeSpan.FromSeconds(3));

        _daemon!.StatusFor(OperationShard).ShouldBe(AgentStatus.Paused);
        _daemon.HasAnyPaused().ShouldBeTrue();
        (await Load(poison)).ShouldBeNull("the unreadable event must not be projected as plaintext, as [SHREDDED] or as an empty row");
        (await Load(after)).ShouldBeNull("the shard stopped: later events wait for the operator");

        // The daemon applies a range of events as one batch, so the healthy event in the failing range is not committed
        // either; it is projected when the operator resolves the failure and the shard runs again.
        (await Load(first)).ShouldBeNull("a failing batch commits nothing");
    }

    private void AssertCapturedFailureWithoutLeaks(string? envelope)
    {
        var captured = string.Join("\n", _logs.Entries);

        captured.ShouldContain(EncryptedFieldDecryptionException.Code, Case.Sensitive);
        captured.ShouldNotContain(Plaintext);
        captured.ShouldNotContain("authentication tag", Case.Insensitive);
        captured.ShouldNotContain("temporal:", Case.Insensitive);
        captured.ShouldNotContain("__enc", Case.Sensitive);
        if (envelope is not null)
        {
            captured.ShouldNotContain(envelope);
        }
    }

    private async Task<byte[]> SeedKeyAsync(string period)
    {
        var key = RandomNumberGenerator.GetBytes(32);
        await using var session = _store!.LightweightSession();
        session.Store(new TemporalKeyDocument
        {
            Id = TemporalPeriodHelper.FormatKeyId(period, 1),
            Period = period,
            KeyMaterial = key,
            Version = 1,
            Status = TemporalKeyStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await session.SaveChangesAsync();
        return key;
    }

    private async Task<Guid> AppendAsync(string period, byte[] key, EncryptedField? userId = null)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await using var session = _store!.LightweightSession();
        session.Events.Append(
            $"audit:Order:{id:N}",
            new OperationAuditEntryRecordedEvent
            {
                Id = id,
                CorrelationId = "corr",
                Action = "Create",
                EntityType = "Order",
                EntityId = id.ToString("N"),
                Outcome = 0,
                TimestampUtc = now,
                StartedAtUtc = now,
                CompletedAtUtc = now,
                EncryptedUserId = userId ?? EncryptedField.Encrypt(Plaintext, key, TemporalPeriodHelper.FormatKeyId(period, 1)),
                TemporalKeyPeriod = period
            });
        await session.SaveChangesAsync();
        return id;
    }

    private async Task DestroyAsync(ErasureReference? erasureReference)
    {
        await using var session = _store!.LightweightSession();
        var keys = new MartenTemporalKeyProvider(
            session, TimeProvider.System, _loggerFactory!.CreateLogger<MartenTemporalKeyProvider>());

        var destroyed = await keys.DestroyKeysBeforeAsync(
            new DateTime(2020, 2, 1, 0, 0, 0, DateTimeKind.Utc), TemporalKeyGranularity.Monthly, erasureReference);

        destroyed.Match(Right: count => count.ShouldBe(1), Left: error => throw new ShouldAssertException($"Destroy failed: {error.GetCode().IfNone("none")}"));
    }

    private async Task StartDaemonAsync()
    {
        _daemon = await _store!.BuildProjectionDaemonAsync(
            StorageConstants.DefaultTenantId, _loggerFactory!.CreateLogger("Marten.Daemon"));
        await _daemon.StartAllAsync();
    }

    private async Task<OperationAuditEntryReadModel?> Load(Guid id)
    {
        await using var session = _store!.QuerySession();
        return await session.LoadAsync<OperationAuditEntryReadModel>(id);
    }

    private async Task<bool> IsProjectedAsync(Guid id) => await Load(id) is not null;

    private async Task WaitForAsync(Func<Task<bool>> condition, TimeSpan? timeout = null)
    {
        var reached = await TryWaitForAsync(condition, timeout ?? TimeSpan.FromSeconds(60));
        reached.ShouldBeTrue("condition was not reached in time; last log lines: " + string.Join(" || ", _logs.Entries.Where(e => !e.Contains("high water mark", StringComparison.Ordinal)).Distinct().Take(15)));
    }

    private static async Task<bool> TryWaitForAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return true;
            }

            await Task.Delay(250);
        }

        return await condition();
    }

    private void SkipWhenUnavailable()
    {
        if (!fixture.IsAvailable)
        {
            Assert.Skip("PostgreSQL through Testcontainers is not available.");
        }
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<string> _entries = new();

        public IReadOnlyCollection<string> Entries => _entries;

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _entries);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(string category, ConcurrentQueue<string> sink) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                sink.Enqueue($"[{category}] {logLevel}: {formatter(state, exception)} {exception}");
        }
    }
}
