using System.Data;

using Encina.Marten.GDPR;

using JasperFx;

using LanguageExt;

using Marten;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Npgsql;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Marten.GDPR;

/// <summary>
/// Unit tests for <see cref="PostgreSqlSubjectKeyProvider"/> with a substituted Marten store (#1699).
/// Paths that need real LINQ queries and real transactions are covered by the PostgreSQL integration tests.
/// </summary>
public sealed class PostgreSqlSubjectKeyProviderTests
{
    private const string TenantId = "tenant-a";
    private const string SubjectId = "subject-1";

    private readonly IDocumentStore _store = Substitute.For<IDocumentStore>();
    private readonly IQuerySession _querySession = Substitute.For<IQuerySession>();
    private readonly IDocumentSession _lockedSession = Substitute.For<IDocumentSession>();
    private readonly PostgreSqlSubjectKeyProvider _sut;

    public PostgreSqlSubjectKeyProviderTests()
    {
        var injectedSession = Substitute.For<IDocumentSession>();
        injectedSession.DocumentStore.Returns(_store);
        injectedSession.TenantId.Returns(TenantId);

        _store.QuerySession(TenantId).Returns(_querySession);
        _store.LightweightSession(TenantId, IsolationLevel.ReadCommitted).Returns(_lockedSession);

        _sut = new PostgreSqlSubjectKeyProvider(
            injectedSession,
            new FakeTimeProvider(),
            NullLogger<PostgreSqlSubjectKeyProvider>.Instance);
    }

    #region ComputeSubjectLockKey

    [Fact]
    public void ComputeSubjectLockKey_SameInput_ReturnsSameKey()
    {
        PostgreSqlSubjectKeyProvider.ComputeSubjectLockKey(TenantId, SubjectId)
            .ShouldBe(PostgreSqlSubjectKeyProvider.ComputeSubjectLockKey(TenantId, SubjectId));
    }

    [Fact]
    public void ComputeSubjectLockKey_DifferentSubjects_ReturnDifferentKeys()
    {
        PostgreSqlSubjectKeyProvider.ComputeSubjectLockKey(TenantId, "subject-1")
            .ShouldNotBe(PostgreSqlSubjectKeyProvider.ComputeSubjectLockKey(TenantId, "subject-2"));
    }

    [Fact]
    public void ComputeSubjectLockKey_DifferentTenants_ReturnDifferentKeys()
    {
        PostgreSqlSubjectKeyProvider.ComputeSubjectLockKey("tenant-a", SubjectId)
            .ShouldNotBe(PostgreSqlSubjectKeyProvider.ComputeSubjectLockKey("tenant-b", SubjectId));
    }

    [Fact]
    public void ComputeSubjectLockKey_SeparatorPreventsConcatenationCollisions()
    {
        // "ab" + "c" and "a" + "bc" must not share a lock key
        PostgreSqlSubjectKeyProvider.ComputeSubjectLockKey("ab", "c")
            .ShouldNotBe(PostgreSqlSubjectKeyProvider.ComputeSubjectLockKey("a", "bc"));
    }

    #endregion

    #region IsDuplicateKeyConflict

    [Fact]
    public void IsDuplicateKeyConflict_UniqueViolation_ReturnsTrue()
    {
        PostgreSqlSubjectKeyProvider.IsDuplicateKeyConflict(UniqueViolation()).ShouldBeTrue();
    }

    [Fact]
    public void IsDuplicateKeyConflict_UniqueViolationAsInnerException_ReturnsTrue()
    {
        var wrapped = new InvalidOperationException("outer", new InvalidOperationException("middle", UniqueViolation()));

        PostgreSqlSubjectKeyProvider.IsDuplicateKeyConflict(wrapped).ShouldBeTrue();
    }

    [Fact]
    public void IsDuplicateKeyConflict_MartenDocumentAlreadyExists_ReturnsTrue()
    {
        var withoutInner = new DocumentAlreadyExistsException(typeof(string), "id-1");
        var withInner = new DocumentAlreadyExistsException(new InvalidOperationException("cause"), typeof(string), "id-1");

        PostgreSqlSubjectKeyProvider.IsDuplicateKeyConflict(withoutInner).ShouldBeTrue();
        PostgreSqlSubjectKeyProvider.IsDuplicateKeyConflict(withInner).ShouldBeTrue();
    }

    [Fact]
    public void IsDuplicateKeyConflict_OtherPostgresError_ReturnsFalse()
    {
        var deadlock = new PostgresException("deadlock", "ERROR", "ERROR", PostgresErrorCodes.DeadlockDetected);

        PostgreSqlSubjectKeyProvider.IsDuplicateKeyConflict(deadlock).ShouldBeFalse();
    }

    [Fact]
    public void IsDuplicateKeyConflict_UnrelatedException_ReturnsFalse()
    {
        PostgreSqlSubjectKeyProvider.IsDuplicateKeyConflict(new InvalidOperationException("boom")).ShouldBeFalse();
    }

    #endregion

    #region Forgotten subject

    [Fact]
    public async Task GetOrCreateSubjectKeyAsync_ForgottenSubject_ReturnsSubjectForgottenWithoutWriting()
    {
        ArrangeForgotten(_querySession);

        var result = await _sut.GetOrCreateSubjectKeyAsync(SubjectId);

        ErrorCode(result).ShouldBe(CryptoShreddingErrors.SubjectForgottenCode);
        _store.DidNotReceive().LightweightSession(Arg.Any<string>(), Arg.Any<IsolationLevel>());
    }

    [Fact]
    public async Task IsSubjectForgottenAsync_MarkerPresent_ReturnsTrue()
    {
        ArrangeForgotten(_querySession);

        var result = await _sut.IsSubjectForgottenAsync(SubjectId);

        result.ShouldBe(Right<EncinaError, bool>(true));
    }

    [Fact]
    public async Task IsSubjectForgottenAsync_NoMarker_ReturnsFalse()
    {
        var result = await _sut.IsSubjectForgottenAsync(SubjectId);

        result.ShouldBe(Right<EncinaError, bool>(false));
    }

    [Fact]
    public async Task GetSubjectKeyAsync_WithVersion_ForgottenSubject_ReturnsSubjectForgotten()
    {
        ArrangeForgotten(_querySession);

        var result = await _sut.GetSubjectKeyAsync(SubjectId, version: 1);

        ErrorCode(result).ShouldBe(CryptoShreddingErrors.SubjectForgottenCode);
    }

    [Fact]
    public async Task GetSubjectKeyAsync_WithoutVersion_ForgottenSubject_ReturnsSubjectForgotten()
    {
        ArrangeForgotten(_querySession);

        var result = await _sut.GetSubjectKeyAsync(SubjectId);

        ErrorCode(result).ShouldBe(CryptoShreddingErrors.SubjectForgottenCode);
    }

    [Fact]
    public async Task GetSubjectInfoAsync_ForgottenSubject_ReturnsForgottenInfo()
    {
        var forgottenAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        _querySession.LoadAsync<SubjectForgottenMarker>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new SubjectForgottenMarker { Id = $"forgotten:{SubjectId}", SubjectId = SubjectId, ForgottenAtUtc = forgottenAt });

        var result = await _sut.GetSubjectInfoAsync(SubjectId);

        var info = result.Match(Right: i => i, Left: _ => throw new ShouldAssertException("expected Right"));
        info.Status.ShouldBe(SubjectStatus.Forgotten);
        info.ForgottenAtUtc.ShouldBe(forgottenAt);
        info.TotalKeyVersions.ShouldBe(0);
    }

    [Fact]
    public async Task RotateSubjectKeyAsync_ForgottenSubject_ReturnsSubjectForgottenUnderTheLock()
    {
        ArrangeForgotten(_lockedSession);

        var result = await _sut.RotateSubjectKeyAsync(SubjectId);

        ErrorCode(result).ShouldBe(CryptoShreddingErrors.SubjectForgottenCode);
        await _lockedSession.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _lockedSession.Received(1).ExecuteAsync(
            Arg.Is<NpgsqlCommand>(c => IsLockCommandFor(c, TenantId, SubjectId)),
            Arg.Any<CancellationToken>());
        await _lockedSession.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Versioned read

    [Fact]
    public async Task GetSubjectKeyAsync_WithVersion_StoredDocument_ReturnsItsKeyMaterial()
    {
        var material = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
        _querySession.LoadAsync<SubjectKeyDocument>($"subject:{SubjectId}:v2", Arg.Any<CancellationToken>())
            .Returns(new SubjectKeyDocument { Id = $"subject:{SubjectId}:v2", SubjectId = SubjectId, Version = 2, KeyMaterial = material });

        var result = await _sut.GetSubjectKeyAsync(SubjectId, version: 2);

        result.Match(Right: k => k, Left: _ => []).ShouldBe(material);
    }

    [Fact]
    public async Task GetSubjectKeyAsync_WithVersion_MissingDocument_ReturnsKeyNotFound()
    {
        var result = await _sut.GetSubjectKeyAsync(SubjectId, version: 3);

        result.IsLeft.ShouldBeTrue();
        ErrorCode(result).ShouldNotBe(CryptoShreddingErrors.SubjectForgottenCode);
    }

    #endregion

    #region Failures stay in the railway

    [Fact]
    public async Task GetOrCreateSubjectKeyAsync_StoreThrows_ReturnsKeyStoreError()
    {
        _store.QuerySession(TenantId).Returns(_ => throw new InvalidOperationException("db down"));

        var result = await _sut.GetOrCreateSubjectKeyAsync(SubjectId);

        ErrorCode(result).ShouldBe(CryptoShreddingErrors.KeyStoreErrorCode);
    }

    [Fact]
    public async Task GetSubjectKeyAsync_StoreThrows_ReturnsKeyStoreError()
    {
        _store.QuerySession(TenantId).Returns(_ => throw new InvalidOperationException("db down"));

        var result = await _sut.GetSubjectKeyAsync(SubjectId, version: 1);

        ErrorCode(result).ShouldBe(CryptoShreddingErrors.KeyStoreErrorCode);
    }

    [Fact]
    public async Task IsSubjectForgottenAsync_StoreThrows_ReturnsKeyStoreError()
    {
        _store.QuerySession(TenantId).Returns(_ => throw new InvalidOperationException("db down"));

        var result = await _sut.IsSubjectForgottenAsync(SubjectId);

        ErrorCode(result).ShouldBe(CryptoShreddingErrors.KeyStoreErrorCode);
    }

    [Fact]
    public async Task GetSubjectInfoAsync_StoreThrows_ReturnsKeyStoreError()
    {
        _store.QuerySession(TenantId).Returns(_ => throw new InvalidOperationException("db down"));

        var result = await _sut.GetSubjectInfoAsync(SubjectId);

        ErrorCode(result).ShouldBe(CryptoShreddingErrors.KeyStoreErrorCode);
    }

    [Fact]
    public async Task DeleteSubjectKeysAsync_LockFails_ReturnsKeyStoreErrorAndDisposesTheSession()
    {
        _lockedSession.ExecuteAsync(Arg.Any<NpgsqlCommand>(), Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new InvalidOperationException("lock failed"));

        var result = await _sut.DeleteSubjectKeysAsync(SubjectId);

        ErrorCode(result).ShouldBe(CryptoShreddingErrors.KeyStoreErrorCode);
        await _lockedSession.Received(1).DisposeAsync();
        await _lockedSession.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RotateSubjectKeyAsync_LockFails_ReturnsKeyRotationFailed()
    {
        _lockedSession.ExecuteAsync(Arg.Any<NpgsqlCommand>(), Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new InvalidOperationException("lock failed"));

        var result = await _sut.RotateSubjectKeyAsync(SubjectId);

        ErrorCode(result).ShouldBe(CryptoShreddingErrors.KeyRotationFailedCode);
        await _lockedSession.Received(1).DisposeAsync();
    }

    #endregion

    private static PostgresException UniqueViolation() =>
        new("duplicate key value violates unique constraint", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation);

    private static void ArrangeForgotten(IQuerySession session) =>
        session.LoadAsync<SubjectForgottenMarker>($"forgotten:{SubjectId}", Arg.Any<CancellationToken>())
            .Returns(new SubjectForgottenMarker { Id = $"forgotten:{SubjectId}", SubjectId = SubjectId });

    private static bool IsLockCommandFor(NpgsqlCommand command, string tenantId, string subjectId) =>
        command.CommandText.Contains("pg_advisory_xact_lock", StringComparison.Ordinal)
        && command.Parameters.Count == 1
        && (long)command.Parameters[0].Value! == PostgreSqlSubjectKeyProvider.ComputeSubjectLockKey(tenantId, subjectId);

    private static string ErrorCode<T>(Either<EncinaError, T> result) =>
        result.Match(
            Right: _ => "<right>",
            Left: error => error.GetCode().IfNone("<no code>"));
}
