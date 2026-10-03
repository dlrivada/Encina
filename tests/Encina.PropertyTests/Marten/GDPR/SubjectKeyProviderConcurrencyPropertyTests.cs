using Encina.Marten.GDPR;

using FsCheck;
using FsCheck.Xunit;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.PropertyTests.Marten.GDPR;

/// <summary>
/// Property-based tests for the key lifecycle under concurrency (#1699): one stored key per subject whatever the
/// number of first writers, one new version per rotation, no key surviving an erasure, and a stable advisory lock
/// key for the PostgreSQL provider.
/// </summary>
[Trait("Category", "Property")]
[Trait("Provider", "Marten")]
public sealed class SubjectKeyProviderConcurrencyPropertyTests
{
    [Property(MaxTest = 30)]
    public bool ConcurrentFirstWriters_AllReturnTheStoredKey(PositiveInt callers)
    {
        var count = 2 + (callers.Get % 15);
        var sut = NewInMemoryProvider();

        var results = RunConcurrently(count, () => sut.GetOrCreateSubjectKeyAsync("subject").AsTask());
        var stored = (byte[])sut.GetSubjectKeyAsync("subject").AsTask().GetAwaiter().GetResult();
        var info = (SubjectEncryptionInfo)sut.GetSubjectInfoAsync("subject").AsTask().GetAwaiter().GetResult();

        return info.TotalKeyVersions == 1
            && results.All(r => r.Match(
                Right: k => k.Version == 1 && k.KeyMaterial.AsSpan().SequenceEqual(stored),
                Left: _ => false));
    }

    [Property(MaxTest = 30)]
    public bool ConcurrentRotations_CreateOneNewVersionEach(PositiveInt rotations)
    {
        var count = 1 + (rotations.Get % 12);
        var sut = NewInMemoryProvider();
        sut.GetOrCreateSubjectKeyAsync("subject").AsTask().GetAwaiter().GetResult();

        var versions = RunConcurrently(count, () => sut.RotateSubjectKeyAsync("subject").AsTask())
            .Select(r => r.Match(Right: x => x.NewVersion, Left: _ => -1))
            .Order()
            .ToArray();
        var info = (SubjectEncryptionInfo)sut.GetSubjectInfoAsync("subject").AsTask().GetAwaiter().GetResult();

        return versions.SequenceEqual(Enumerable.Range(2, count))
            && info.ActiveKeyVersion == count + 1
            && info.TotalKeyVersions == count + 1;
    }

    [Property(MaxTest = 30)]
    public bool WritersRacingAnErasure_LeaveNoKey(PositiveInt writers)
    {
        var count = 1 + (writers.Get % 8);
        var sut = NewInMemoryProvider();

        var writerTask = Task.Run(() => RunConcurrently(count, () => sut.GetOrCreateSubjectKeyAsync("subject").AsTask()));
        var erasure = sut.DeleteSubjectKeysAsync("subject").AsTask().GetAwaiter().GetResult();
        writerTask.GetAwaiter().GetResult();

        var info = (SubjectEncryptionInfo)sut.GetSubjectInfoAsync("subject").AsTask().GetAwaiter().GetResult();
        return erasure.IsRight
            && info.Status == SubjectStatus.Forgotten
            && info.TotalKeyVersions == 0
            && sut.GetOrCreateSubjectKeyAsync("subject").AsTask().GetAwaiter().GetResult().IsLeft;
    }

    [Property(MaxTest = 50)]
    public bool ReturnedKeyMaterial_IsNeverZeroedByErasure(NonEmptyString subjectId)
    {
        var id = subjectId.Get.Trim();
        if (string.IsNullOrWhiteSpace(id))
        {
            return true;
        }

        var sut = NewInMemoryProvider();
        var key = (SubjectEncryptionKey)sut.GetOrCreateSubjectKeyAsync(id).AsTask().GetAwaiter().GetResult();
        var before = key.KeyMaterial.ToArray();

        sut.DeleteSubjectKeysAsync(id).AsTask().GetAwaiter().GetResult();

        return key.KeyMaterial.AsSpan().SequenceEqual(before);
    }

    [Property(MaxTest = 100)]
    public bool AdvisoryLockKey_IsDeterministic(NonNull<string> tenantId, NonNull<string> subjectId) =>
        PostgreSqlSubjectKeyProvider.ComputeSubjectLockKey(tenantId.Get, subjectId.Get)
        == PostgreSqlSubjectKeyProvider.ComputeSubjectLockKey(tenantId.Get, subjectId.Get);

    [Property(MaxTest = 100)]
    public bool AdvisoryLockKey_DiffersBetweenSubjects(NonNull<string> tenantId, NonNull<string> first, NonNull<string> second) =>
        first.Get == second.Get
        || PostgreSqlSubjectKeyProvider.ComputeSubjectLockKey(tenantId.Get, first.Get)
            != PostgreSqlSubjectKeyProvider.ComputeSubjectLockKey(tenantId.Get, second.Get);

    private static InMemorySubjectKeyProvider NewInMemoryProvider() =>
        new(TimeProvider.System, NullLogger<InMemorySubjectKeyProvider>.Instance);

    private static T[] RunConcurrently<T>(int callers, Func<Task<T>> operation)
    {
        using var start = new ManualResetEventSlim(false);
        var tasks = Enumerable.Range(0, callers)
            .Select(_ => Task.Run(() =>
            {
                start.Wait();
                return operation();
            }))
            .ToArray();

        start.Set();
        return Task.WhenAll(tasks).GetAwaiter().GetResult();
    }
}
