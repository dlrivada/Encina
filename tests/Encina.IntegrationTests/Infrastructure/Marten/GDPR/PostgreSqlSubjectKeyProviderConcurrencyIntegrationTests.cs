using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten.GDPR;

using LanguageExt;

using Marten;

using Microsoft.Extensions.Logging.Abstractions;

using Npgsql;

using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.Marten.GDPR;

/// <summary>
/// Concurrency integration tests for <see cref="PostgreSqlSubjectKeyProvider"/> on a real PostgreSQL instance (#1699):
/// concurrent first writers, concurrent rotations, writes during an erasure, repeated erasure, and the insert
/// conflict with a writer that does not take the subject lock.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class PostgreSqlSubjectKeyProviderConcurrencyIntegrationTests : IAsyncLifetime
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(30);

    private readonly MartenFixture _fixture;
    private readonly List<IDocumentSession> _sessions = [];

    public PostgreSqlSubjectKeyProviderConcurrencyIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "Marten PostgreSQL container not available");
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var session in _sessions)
        {
            await session.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetOrCreateSubjectKeyAsync_ConcurrentFirstWriters_StoreOneKeyAndEveryCallerGetsIt()
    {
        // Arrange: each caller has its own provider and session, as separate requests would
        var subjectId = NewSubjectId();
        var providers = Enumerable.Range(0, 16).Select(_ => NewProvider()).ToArray();

        // Act
        var results = await RunConcurrentlyAsync(providers, p => p.GetOrCreateSubjectKeyAsync(subjectId).AsTask());

        // Assert
        var stored = await StoredKeysAsync(subjectId);
        stored.Count.ShouldBe(1);
        foreach (var result in results)
        {
            var key = (SubjectEncryptionKey)result;
            key.Version.ShouldBe(1);
            key.KeyMaterial.ShouldBe(stored[0].KeyMaterial);
        }
    }

    [Fact]
    public async Task RotateSubjectKeyAsync_ConcurrentRotations_EachCreatesOneNewVersion()
    {
        // Arrange
        var subjectId = NewSubjectId();
        (await NewProvider().GetOrCreateSubjectKeyAsync(subjectId)).IsRight.ShouldBeTrue();
        const int rotations = 8;
        var providers = Enumerable.Range(0, rotations).Select(_ => NewProvider()).ToArray();

        // Act
        var results = await RunConcurrentlyAsync(providers, p => p.RotateSubjectKeyAsync(subjectId).AsTask());

        // Assert: no two rotations wrote the same version, and one version is active
        results.Select(r => ((KeyRotationResult)r).NewVersion).Order().ShouldBe(Enumerable.Range(2, rotations));
        var stored = await StoredKeysAsync(subjectId);
        stored.Count.ShouldBe(rotations + 1);
        stored.Where(k => k.Status == SubjectKeyStatus.Active).Select(k => k.Version).ShouldBe([rotations + 1]);
    }

    [Fact]
    public async Task GetOrCreateSubjectKeyAsync_WriterWaitingOnAnErasure_DoesNotCreateAKeyAfterItCommits()
    {
        // Arrange: an erasure holds the subject lock and has written the forgotten marker, not yet committed
        var subjectId = NewSubjectId();
        await using var erasure = _fixture.Store!.LightweightSession();
        await erasure.BeginTransactionAsync(CancellationToken.None);
        await TakeSubjectLockAsync(erasure, subjectId);
        erasure.Store(new SubjectForgottenMarker
        {
            Id = $"forgotten:{subjectId}",
            SubjectId = subjectId,
            ForgottenAtUtc = DateTimeOffset.UnixEpoch
        });

        // Act: the writer passed the unlocked marker check and now waits for the lock
        var writer = NewProvider().GetOrCreateSubjectKeyAsync(subjectId).AsTask();
        await WaitUntilLockHasWaiterAsync(subjectId);
        await erasure.SaveChangesAsync();
        var result = await writer.WaitAsync(WaitTimeout);

        // Assert
        ErrorCode(result).ShouldBe(CryptoShreddingErrors.SubjectForgottenCode);
        (await StoredKeysAsync(subjectId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteSubjectKeysAsync_RacingWriters_NoKeySurvivesTheErasure()
    {
        for (var i = 0; i < 10; i++)
        {
            // Arrange
            var subjectId = NewSubjectId();
            var writers = Enumerable.Range(0, 4).Select(_ => NewProvider()).ToArray();
            var eraser = NewProvider();

            // Act
            var writing = RunConcurrentlyAsync(writers, p => p.GetOrCreateSubjectKeyAsync(subjectId).AsTask());
            var erasing = eraser.DeleteSubjectKeysAsync(subjectId).AsTask();
            await Task.WhenAll(writing, erasing).WaitAsync(WaitTimeout);

            // Assert: every writer got a key or the subject_forgotten error, never any other failure
            foreach (var writerResult in await writing)
            {
                if (writerResult.IsLeft)
                {
                    ErrorCode(writerResult).ShouldBe(CryptoShreddingErrors.SubjectForgottenCode);
                }
            }

            // Whatever the interleaving, the subject ends forgotten with no key document
            (await erasing).IsRight.ShouldBeTrue();
            (await StoredKeysAsync(subjectId)).ShouldBeEmpty();
            ((bool)await eraser.IsSubjectForgottenAsync(subjectId)).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task DeleteSubjectKeysAsync_RepeatedErasure_DeletesLeftoverKeys()
    {
        // Arrange: a forgotten subject with a key document left behind (a writer that bypassed the lock)
        var subjectId = NewSubjectId();
        var sut = NewProvider();
        await sut.GetOrCreateSubjectKeyAsync(subjectId);
        (await sut.DeleteSubjectKeysAsync(subjectId)).IsRight.ShouldBeTrue();
        await StoreDirectlyAsync(KeyDocument(subjectId, version: 1));
        (await StoredKeysAsync(subjectId)).Count.ShouldBe(1);

        // Act
        var result = await sut.DeleteSubjectKeysAsync(subjectId);

        // Assert
        ErrorCode(result).ShouldBe(CryptoShreddingErrors.SubjectForgottenCode);
        (await StoredKeysAsync(subjectId)).ShouldBeEmpty();
        ((bool)await sut.IsSubjectForgottenAsync(subjectId)).ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteSubjectKeysAsync_RepeatedErasureWithoutLeftovers_ReturnsSubjectForgotten()
    {
        // Arrange
        var subjectId = NewSubjectId();
        var sut = NewProvider();
        await sut.GetOrCreateSubjectKeyAsync(subjectId);
        await sut.DeleteSubjectKeysAsync(subjectId);

        // Act
        var result = await sut.DeleteSubjectKeysAsync(subjectId);

        // Assert
        ErrorCode(result).ShouldBe(CryptoShreddingErrors.SubjectForgottenCode);
        (await StoredKeysAsync(subjectId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task GetOrCreateSubjectKeyAsync_InsertConflictWithAnUnlockedWriter_ReturnsTheStoredWinner()
    {
        // Arrange: a writer that does not take the lock stores version 1 just before the provider saves
        var subjectId = NewSubjectId();
        var winner = KeyDocument(subjectId, version: 1);
        var listener = new RacingWriterListener(_fixture.Store!, winner);
        using var store = StoreWithListener(listener);
        await using var session = store.LightweightSession();
        var sut = new PostgreSqlSubjectKeyProvider(session, TimeProvider.System, NullLogger<PostgreSqlSubjectKeyProvider>.Instance);

        // Act
        var result = await sut.GetOrCreateSubjectKeyAsync(subjectId);

        // Assert
        listener.Fired.ShouldBeTrue();
        var key = (SubjectEncryptionKey)result;
        key.Version.ShouldBe(1);
        key.KeyMaterial.ShouldBe(winner.KeyMaterial);
        (await StoredKeysAsync(subjectId)).Single().KeyMaterial.ShouldBe(winner.KeyMaterial);
    }

    [Fact]
    public async Task RotateSubjectKeyAsync_InsertConflictWithAnUnlockedWriter_ReportsTheStoredVersion()
    {
        // Arrange
        var subjectId = NewSubjectId();
        (await NewProvider().GetOrCreateSubjectKeyAsync(subjectId)).IsRight.ShouldBeTrue();
        var winner = KeyDocument(subjectId, version: 2);
        var listener = new RacingWriterListener(_fixture.Store!, winner);
        using var store = StoreWithListener(listener);
        await using var session = store.LightweightSession();
        var sut = new PostgreSqlSubjectKeyProvider(session, TimeProvider.System, NullLogger<PostgreSqlSubjectKeyProvider>.Instance);

        // Act
        var result = await sut.RotateSubjectKeyAsync(subjectId);

        // Assert
        listener.Fired.ShouldBeTrue();
        var rotation = (KeyRotationResult)result;
        rotation.OldVersion.ShouldBe(1);
        rotation.NewVersion.ShouldBe(2);
        rotation.NewKeyId.ShouldBe(winner.Id);
        var stored = await StoredKeysAsync(subjectId);
        stored.Single(k => k.Version == 2).KeyMaterial.ShouldBe(winner.KeyMaterial);
        ((byte[])await sut.GetSubjectKeyAsync(subjectId)).ShouldBe(winner.KeyMaterial);
    }

    [Fact]
    public async Task GetOrCreateSubjectKeyAsync_ConflictWhoseWinnerIsNotAKeyOfTheSubject_FailsClosed()
    {
        // Arrange: the conflicting document has the key id but belongs to no active key of the subject,
        // so the reload finds no stored winner
        var subjectId = NewSubjectId();
        var foreign = KeyDocument(subjectId, version: 1);
        foreign.SubjectId = NewSubjectId();
        var listener = new RacingWriterListener(_fixture.Store!, foreign);
        using var store = StoreWithListener(listener);
        await using var session = store.LightweightSession();
        var sut = new PostgreSqlSubjectKeyProvider(session, TimeProvider.System, NullLogger<PostgreSqlSubjectKeyProvider>.Instance);

        // Act
        var result = await sut.GetOrCreateSubjectKeyAsync(subjectId);

        // Assert: never a key that is not stored for the subject
        listener.Fired.ShouldBeTrue();
        ErrorCode(result).ShouldBe(CryptoShreddingErrors.KeyStoreErrorCode);
        (await StoredKeysAsync(subjectId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Tenants_ErasureInOneTenant_NeitherBlocksNorErasesTheSameSubjectInAnother()
    {
        // Arrange: a conjoined multi-tenant store in its own schema; tenant A erases while holding its lock
        using var store = DocumentStore.For(opts =>
        {
            opts.Connection(_fixture.ConnectionString);
            opts.DatabaseSchemaName = "gdpr_key_tenancy";
            opts.Policies.AllDocumentsAreMultiTenanted();
        });
        var subjectId = NewSubjectId();
        await using var tenantASession = store.LightweightSession("tenant-a");
        await using var tenantBSession = store.LightweightSession("tenant-b");
        var tenantA = new PostgreSqlSubjectKeyProvider(tenantASession, TimeProvider.System, NullLogger<PostgreSqlSubjectKeyProvider>.Instance);
        var tenantB = new PostgreSqlSubjectKeyProvider(tenantBSession, TimeProvider.System, NullLogger<PostgreSqlSubjectKeyProvider>.Instance);
        (await tenantA.GetOrCreateSubjectKeyAsync(subjectId)).IsRight.ShouldBeTrue();

        await using var erasure = store.LightweightSession("tenant-a");
        await erasure.BeginTransactionAsync(CancellationToken.None);
        await TakeSubjectLockAsync(erasure, subjectId);

        // Act: tenant B is not serialized behind tenant A's lock
        var tenantBKey = await tenantB.GetOrCreateSubjectKeyAsync(subjectId).AsTask().WaitAsync(WaitTimeout);
        await erasure.DisposeAsync();
        var erased = await tenantA.DeleteSubjectKeysAsync(subjectId);

        // Assert
        tenantBKey.IsRight.ShouldBeTrue();
        erased.IsRight.ShouldBeTrue();
        ErrorCode(await tenantA.GetOrCreateSubjectKeyAsync(subjectId)).ShouldBe(CryptoShreddingErrors.SubjectForgottenCode);
        ((byte[])await tenantB.GetSubjectKeyAsync(subjectId)).ShouldBe(((SubjectEncryptionKey)tenantBKey).KeyMaterial);
        ((bool)await tenantB.IsSubjectForgottenAsync(subjectId)).ShouldBeFalse();
    }

    [Fact]
    public async Task RotateSubjectKeyAsync_WithoutKey_ReturnsKeyNotFound()
    {
        // Act
        var result = await NewProvider().RotateSubjectKeyAsync(NewSubjectId());

        // Assert
        result.IsLeft.ShouldBeTrue();
        ErrorCode(result).ShouldNotBe(CryptoShreddingErrors.KeyRotationFailedCode);
    }

    [Fact]
    public async Task RotateSubjectKeyAsync_ForgottenSubject_ReturnsSubjectForgotten()
    {
        // Arrange
        var subjectId = NewSubjectId();
        var sut = NewProvider();
        await sut.GetOrCreateSubjectKeyAsync(subjectId);
        await sut.DeleteSubjectKeysAsync(subjectId);

        // Act
        var result = await sut.RotateSubjectKeyAsync(subjectId);

        // Assert
        ErrorCode(result).ShouldBe(CryptoShreddingErrors.SubjectForgottenCode);
        (await StoredKeysAsync(subjectId)).ShouldBeEmpty();
    }

    private PostgreSqlSubjectKeyProvider NewProvider()
    {
        var session = _fixture.Store!.LightweightSession();
        _sessions.Add(session);
        return new PostgreSqlSubjectKeyProvider(session, TimeProvider.System, NullLogger<PostgreSqlSubjectKeyProvider>.Instance);
    }

    private DocumentStore StoreWithListener(RacingWriterListener listener) =>
        DocumentStore.For(opts =>
        {
            opts.Connection(_fixture.ConnectionString);
            opts.Listeners.Add(listener);
        });

    private async Task<IReadOnlyList<SubjectKeyDocument>> StoredKeysAsync(string subjectId)
    {
        await using var session = _fixture.Store!.QuerySession();
        return await session.Query<SubjectKeyDocument>()
            .Where(d => d.SubjectId == subjectId)
            .ToListAsync();
    }

    private async Task StoreDirectlyAsync(SubjectKeyDocument document)
    {
        await using var session = _fixture.Store!.LightweightSession();
        session.Store(document);
        await session.SaveChangesAsync();
    }

    private static async Task TakeSubjectLockAsync(IDocumentSession session, string subjectId)
    {
        using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(@lockKey)");
        command.Parameters.AddWithValue("lockKey", PostgreSqlSubjectKeyProvider.ComputeSubjectLockKey(session.TenantId, subjectId));
        await session.ExecuteAsync(command, CancellationToken.None);
    }

    /// <summary>
    /// Waits until another connection is blocked on the subject's advisory lock (a 64-bit key is stored in
    /// pg_locks as classid = high 32 bits, objid = low 32 bits).
    /// </summary>
    private async Task WaitUntilLockHasWaiterAsync(string subjectId, string tenantId = "*DEFAULT*")
    {
        var key = PostgreSqlSubjectKeyProvider.ComputeSubjectLockKey(tenantId, subjectId);
        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT count(*) FROM pg_locks WHERE locktype = 'advisory' AND NOT granted "
            + "AND classid::bigint = @high AND objid::bigint = @low",
            connection);
        command.Parameters.AddWithValue("high", (long)(uint)(key >> 32));
        command.Parameters.AddWithValue("low", (long)(uint)key);

        using var timeout = new CancellationTokenSource(WaitTimeout);
        while ((long)(await command.ExecuteScalarAsync(timeout.Token))! == 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
        }
    }

    private static async Task<T[]> RunConcurrentlyAsync<T>(
        IEnumerable<PostgreSqlSubjectKeyProvider> providers,
        Func<PostgreSqlSubjectKeyProvider, Task<T>> operation)
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = providers
            .Select(p => Task.Run(async () =>
            {
                await start.Task;
                return await operation(p);
            }))
            .ToArray();

        start.SetResult();
        return await Task.WhenAll(tasks).WaitAsync(WaitTimeout);
    }

    private static SubjectKeyDocument KeyDocument(string subjectId, int version)
    {
        var material = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(material);
        return new SubjectKeyDocument
        {
            Id = $"subject:{subjectId}:v{version}",
            SubjectId = subjectId,
            KeyMaterial = material,
            Version = version,
            Status = SubjectKeyStatus.Active,
            CreatedAtUtc = DateTimeOffset.UnixEpoch
        };
    }

    private static string NewSubjectId() => $"race-{Guid.NewGuid():N}";

    private static string ErrorCode<T>(Either<EncinaError, T> result) =>
        result.Match(
            Right: _ => "<right>",
            Left: error => error.GetCode().IfNone("<no code>"));

    /// <summary>
    /// Stores <paramref name="winner"/> through another store, without the subject lock, the first time a
    /// session of the listened store saves: the provider's insert then hits an existing document id.
    /// </summary>
    private sealed class RacingWriterListener(IDocumentStore racerStore, SubjectKeyDocument winner) : DocumentSessionListenerBase
    {
        private int _armed = 1;

        public bool Fired => Volatile.Read(ref _armed) == 0;

        public override async Task BeforeSaveChangesAsync(IDocumentSession session, CancellationToken token)
        {
            if (Interlocked.Exchange(ref _armed, 0) == 1)
            {
                await using var racer = racerStore.LightweightSession();
                racer.Store(winner);
                await racer.SaveChangesAsync(token);
            }
        }
    }
}
