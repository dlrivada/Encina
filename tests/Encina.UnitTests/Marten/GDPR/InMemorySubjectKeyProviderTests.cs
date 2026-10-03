using Encina;
using Encina.Marten.GDPR;
using Encina.Marten.GDPR.Abstractions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Encina.UnitTests.Marten.GDPR;

public sealed class InMemorySubjectKeyProviderTests
{
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly InMemorySubjectKeyProvider _sut;

    public InMemorySubjectKeyProviderTests()
    {
        _sut = new InMemorySubjectKeyProvider(
            _timeProvider,
            NullLogger<InMemorySubjectKeyProvider>.Instance);
    }

    [Fact]
    public async Task GetOrCreateSubjectKeyAsync_NewSubject_CreatesKey()
    {
        // Act
        var result = await _sut.GetOrCreateSubjectKeyAsync("user-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(key =>
        {
            key.ShouldNotBeNull();
            key.KeyMaterial.Length.ShouldBe(32); // AES-256 = 32 bytes
            key.Version.ShouldBe(1);
        });
    }

    [Fact]
    public async Task GetOrCreateSubjectKeyAsync_AfterRotation_ReturnsTheActiveKeyWithItsVersion()
    {
        // Arrange (#1646): the version comes with the key material, never from a second lookup
        await _sut.GetOrCreateSubjectKeyAsync("user-1");
        await _sut.RotateSubjectKeyAsync("user-1");
        var v2 = (byte[])await _sut.GetSubjectKeyAsync("user-1", version: 2);

        // Act
        var result = await _sut.GetOrCreateSubjectKeyAsync("user-1");

        // Assert
        var key = (SubjectEncryptionKey)result;
        key.Version.ShouldBe(2);
        key.KeyMaterial.ShouldBe(v2);
    }

    [Fact]
    public async Task GetOrCreateSubjectKeyAsync_ExistingSubject_ReturnsSameKey()
    {
        // Arrange
        var first = await _sut.GetOrCreateSubjectKeyAsync("user-1");

        // Act
        var second = await _sut.GetOrCreateSubjectKeyAsync("user-1");

        // Assert
        first.IsRight.ShouldBeTrue();
        second.IsRight.ShouldBeTrue();
        first.IfRight(k1 => second.IfRight(k2 =>
        {
            k2.Version.ShouldBe(k1.Version);
            k2.KeyMaterial.ShouldBe(k1.KeyMaterial);
        }));
    }

    [Fact]
    public async Task GetOrCreateSubjectKeyAsync_ForgottenSubject_ReturnsError()
    {
        // Arrange
        await _sut.GetOrCreateSubjectKeyAsync("user-1");
        await _sut.DeleteSubjectKeysAsync("user-1");

        // Act
        var result = await _sut.GetOrCreateSubjectKeyAsync("user-1");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(err => err.GetEncinaCode().ShouldBe(CryptoShreddingErrors.SubjectForgottenCode));
    }

    [Fact]
    public async Task GetSubjectKeyAsync_ExistingSubject_ReturnsActiveKey()
    {
        // Arrange
        var created = await _sut.GetOrCreateSubjectKeyAsync("user-1");

        // Act
        var result = await _sut.GetSubjectKeyAsync("user-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        created.IfRight(k1 => result.IfRight(k2 => k1.KeyMaterial.ShouldBe(k2)));
    }

    [Fact]
    public async Task GetSubjectKeyAsync_SpecificVersion_ReturnsCorrectKey()
    {
        // Arrange
        await _sut.GetOrCreateSubjectKeyAsync("user-1");
        await _sut.RotateSubjectKeyAsync("user-1");

        // Act
        var v1 = await _sut.GetSubjectKeyAsync("user-1", version: 1);
        var v2 = await _sut.GetSubjectKeyAsync("user-1", version: 2);

        // Assert
        v1.IsRight.ShouldBeTrue();
        v2.IsRight.ShouldBeTrue();
        v1.IfRight(k1 => v2.IfRight(k2 => k1.ShouldNotBe(k2)));
    }

    [Fact]
    public async Task GetSubjectKeyAsync_NonExistentSubject_ReturnsError()
    {
        // Act
        var result = await _sut.GetSubjectKeyAsync("non-existent");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetSubjectKeyAsync_ForgottenSubject_ReturnsError()
    {
        // Arrange
        await _sut.GetOrCreateSubjectKeyAsync("user-1");
        await _sut.DeleteSubjectKeysAsync("user-1");

        // Act
        var result = await _sut.GetSubjectKeyAsync("user-1");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(err => err.GetEncinaCode().ShouldBe(CryptoShreddingErrors.SubjectForgottenCode));
    }

    [Fact]
    public async Task DeleteSubjectKeysAsync_ExistingSubject_DeletesKeys()
    {
        // Arrange
        await _sut.GetOrCreateSubjectKeyAsync("user-1");

        // Act
        var result = await _sut.DeleteSubjectKeysAsync("user-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(r =>
        {
            r.SubjectId.ShouldBe("user-1");
            r.KeysDeleted.ShouldBe(1);
        });
    }

    [Fact]
    public async Task DeleteSubjectKeysAsync_AlreadyForgotten_ReturnsError()
    {
        // Arrange
        await _sut.GetOrCreateSubjectKeyAsync("user-1");
        await _sut.DeleteSubjectKeysAsync("user-1");

        // Act
        var result = await _sut.DeleteSubjectKeysAsync("user-1");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(err => err.GetEncinaCode().ShouldBe(CryptoShreddingErrors.SubjectForgottenCode));
    }

    [Fact]
    public async Task IsSubjectForgottenAsync_NotForgotten_ReturnsFalse()
    {
        // Arrange
        await _sut.GetOrCreateSubjectKeyAsync("user-1");

        // Act
        var result = await _sut.IsSubjectForgottenAsync("user-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(isForgotten => isForgotten.ShouldBeFalse());
    }

    [Fact]
    public async Task IsSubjectForgottenAsync_Forgotten_ReturnsTrue()
    {
        // Arrange
        await _sut.GetOrCreateSubjectKeyAsync("user-1");
        await _sut.DeleteSubjectKeysAsync("user-1");

        // Act
        var result = await _sut.IsSubjectForgottenAsync("user-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(isForgotten => isForgotten.ShouldBeTrue());
    }

    [Fact]
    public async Task IsSubjectForgottenAsync_UnknownSubject_ReturnsFalse()
    {
        // Act
        var result = await _sut.IsSubjectForgottenAsync("unknown");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(isForgotten => isForgotten.ShouldBeFalse());
    }

    [Fact]
    public async Task RotateSubjectKeyAsync_ExistingSubject_CreatesNewVersion()
    {
        // Arrange
        await _sut.GetOrCreateSubjectKeyAsync("user-1");

        // Act
        var result = await _sut.RotateSubjectKeyAsync("user-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(r =>
        {
            r.SubjectId.ShouldBe("user-1");
            r.OldVersion.ShouldBe(1);
            r.NewVersion.ShouldBe(2);
            r.OldKeyId.ShouldBe("subject:user-1:v1");
            r.NewKeyId.ShouldBe("subject:user-1:v2");
        });
    }

    [Fact]
    public async Task RotateSubjectKeyAsync_ForgottenSubject_ReturnsError()
    {
        // Arrange
        await _sut.GetOrCreateSubjectKeyAsync("user-1");
        await _sut.DeleteSubjectKeysAsync("user-1");

        // Act
        var result = await _sut.RotateSubjectKeyAsync("user-1");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(err => err.GetEncinaCode().ShouldBe(CryptoShreddingErrors.SubjectForgottenCode));
    }

    [Fact]
    public async Task RotateSubjectKeyAsync_NonExistentSubject_ReturnsError()
    {
        // Act
        var result = await _sut.RotateSubjectKeyAsync("unknown");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetSubjectInfoAsync_ActiveSubject_ReturnsInfo()
    {
        // Arrange
        await _sut.GetOrCreateSubjectKeyAsync("user-1");

        // Act
        var result = await _sut.GetSubjectInfoAsync("user-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(info =>
        {
            info.SubjectId.ShouldBe("user-1");
            info.Status.ShouldBe(SubjectStatus.Active);
            info.ActiveKeyVersion.ShouldBe(1);
            info.TotalKeyVersions.ShouldBe(1);
            info.ForgottenAtUtc.ShouldBeNull();
        });
    }

    [Fact]
    public async Task GetSubjectInfoAsync_ForgottenSubject_ReturnsForgottenStatus()
    {
        // Arrange
        await _sut.GetOrCreateSubjectKeyAsync("user-1");
        await _sut.DeleteSubjectKeysAsync("user-1");

        // Act
        var result = await _sut.GetSubjectInfoAsync("user-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(info =>
        {
            info.SubjectId.ShouldBe("user-1");
            info.Status.ShouldBe(SubjectStatus.Forgotten);
            info.ActiveKeyVersion.ShouldBe(0);
            info.TotalKeyVersions.ShouldBe(0);
        });
    }

    [Fact]
    public async Task GetSubjectInfoAsync_NonExistentSubject_ReturnsError()
    {
        // Act
        var result = await _sut.GetSubjectInfoAsync("unknown");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(err => err.GetEncinaCode().ShouldBe(CryptoShreddingErrors.InvalidSubjectIdCode));
    }

    [Fact]
    public async Task RotateSubjectKeyAsync_MultipleRotations_IncrementsVersion()
    {
        // Arrange
        await _sut.GetOrCreateSubjectKeyAsync("user-1");
        await _sut.RotateSubjectKeyAsync("user-1");

        // Act
        var result = await _sut.RotateSubjectKeyAsync("user-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(r =>
        {
            r.OldVersion.ShouldBe(2);
            r.NewVersion.ShouldBe(3);
        });
    }

    [Fact]
    public async Task SubjectCount_TracksActiveSubjects()
    {
        // Arrange
        await _sut.GetOrCreateSubjectKeyAsync("user-1");
        await _sut.GetOrCreateSubjectKeyAsync("user-2");

        // Assert
        _sut.SubjectCount.ShouldBe(2);
    }

    [Fact]
    public async Task Clear_RemovesAllSubjects()
    {
        // Arrange
        await _sut.GetOrCreateSubjectKeyAsync("user-1");
        await _sut.GetOrCreateSubjectKeyAsync("user-2");

        // Act
        _sut.Clear();

        // Assert
        _sut.SubjectCount.ShouldBe(0);
    }

    [Fact]
    public async Task ConcurrentAccess_DifferentSubjects_AllSucceed()
    {
        // Act
        var tasks = Enumerable.Range(1, 50)
            .Select(i => _sut.GetOrCreateSubjectKeyAsync($"user-{i}").AsTask());
        var results = await Task.WhenAll(tasks);

        // Assert
        results.ShouldAllBe(r => r.IsRight);
        _sut.SubjectCount.ShouldBe(50);
    }

    // -- Races on one subject (#1699) --

    [Fact]
    public async Task GetOrCreateSubjectKeyAsync_ConcurrentFirstWriters_AllGetTheOneStoredKey()
    {
        // Act
        var results = await RunConcurrentlyAsync(32, () => _sut.GetOrCreateSubjectKeyAsync("user-race").AsTask());

        // Assert
        var stored = (byte[])await _sut.GetSubjectKeyAsync("user-race");
        foreach (var result in results)
        {
            var key = (SubjectEncryptionKey)result;
            key.Version.ShouldBe(1);
            key.KeyMaterial.ShouldBe(stored);
        }

        ((SubjectEncryptionInfo)await _sut.GetSubjectInfoAsync("user-race")).TotalKeyVersions.ShouldBe(1);
    }

    [Fact]
    public async Task RotateSubjectKeyAsync_ConcurrentRotations_EachCreatesExactlyOneNewVersion()
    {
        // Arrange
        await _sut.GetOrCreateSubjectKeyAsync("user-rotate");
        const int rotations = 16;

        // Act
        var results = await RunConcurrentlyAsync(rotations, () => _sut.RotateSubjectKeyAsync("user-rotate").AsTask());

        // Assert
        results.Select(r => ((KeyRotationResult)r).NewVersion).Order()
            .ShouldBe(Enumerable.Range(2, rotations));
        var info = (SubjectEncryptionInfo)await _sut.GetSubjectInfoAsync("user-rotate");
        info.ActiveKeyVersion.ShouldBe(rotations + 1);
        info.TotalKeyVersions.ShouldBe(rotations + 1);
    }

    [Fact]
    public async Task GetOrCreateSubjectKeyAsync_RacingErasure_NoKeySurvivesTheErasure()
    {
        for (var i = 0; i < 50; i++)
        {
            // Arrange
            var subjectId = $"user-erase-{i}";

            // Act
            var writers = RunConcurrentlyAsync(8, () => _sut.GetOrCreateSubjectKeyAsync(subjectId).AsTask());
            var erasure = _sut.DeleteSubjectKeysAsync(subjectId).AsTask();
            await Task.WhenAll(writers, erasure);

            // Assert: whatever the interleaving, the subject ends forgotten with no key left
            (await erasure).IsRight.ShouldBeTrue();
            var info = (SubjectEncryptionInfo)await _sut.GetSubjectInfoAsync(subjectId);
            info.Status.ShouldBe(SubjectStatus.Forgotten);
            info.TotalKeyVersions.ShouldBe(0);
            (await _sut.GetOrCreateSubjectKeyAsync(subjectId)).IsLeft.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task DeleteSubjectKeysAsync_DoesNotZeroKeyMaterialAlreadyReturnedToACaller()
    {
        // Arrange: a writer holds the key while the subject is erased
        var held = (SubjectEncryptionKey)await _sut.GetOrCreateSubjectKeyAsync("user-held");
        var copy = held.KeyMaterial.ToArray();

        // Act
        await _sut.DeleteSubjectKeysAsync("user-held");

        // Assert: the writer never encrypts with an all-zero key
        held.KeyMaterial.ShouldBe(copy);
        held.KeyMaterial.ShouldContain(b => b != 0);
    }

    [Fact]
    public async Task GetOrCreateSubjectKeyAsync_ReturnsACopy_CallerMutationDoesNotChangeTheStoredKey()
    {
        // Arrange
        var first = (SubjectEncryptionKey)await _sut.GetOrCreateSubjectKeyAsync("user-copy");
        var original = first.KeyMaterial.ToArray();

        // Act
        Array.Clear(first.KeyMaterial);

        // Assert
        ((SubjectEncryptionKey)await _sut.GetOrCreateSubjectKeyAsync("user-copy")).KeyMaterial.ShouldBe(original);
        ((byte[])await _sut.GetSubjectKeyAsync("user-copy")).ShouldBe(original);
        ((byte[])await _sut.GetSubjectKeyAsync("user-copy", version: 1)).ShouldBe(original);
    }

    [Fact]
    public async Task GetSubjectKeyAsync_ReturnsACopy_CallerMutationDoesNotChangeTheStoredKey()
    {
        // Arrange
        await _sut.GetOrCreateSubjectKeyAsync("user-copy-2");
        var original = (byte[])await _sut.GetSubjectKeyAsync("user-copy-2");

        // Act
        Array.Clear((byte[])await _sut.GetSubjectKeyAsync("user-copy-2"));
        Array.Clear((byte[])await _sut.GetSubjectKeyAsync("user-copy-2", version: 1));

        // Assert
        ((byte[])await _sut.GetSubjectKeyAsync("user-copy-2")).ShouldBe(original);
    }

    [Fact]
    public async Task Clear_DoesNotZeroKeyMaterialAlreadyReturnedToACaller()
    {
        // Arrange
        var held = (SubjectEncryptionKey)await _sut.GetOrCreateSubjectKeyAsync("user-clear");
        var copy = held.KeyMaterial.ToArray();

        // Act
        _sut.Clear();

        // Assert
        held.KeyMaterial.ShouldBe(copy);
        _sut.SubjectCount.ShouldBe(0);
    }

    private static async Task<T[]> RunConcurrentlyAsync<T>(int callers, Func<Task<T>> operation)
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
        return await Task.WhenAll(tasks);
    }
}
