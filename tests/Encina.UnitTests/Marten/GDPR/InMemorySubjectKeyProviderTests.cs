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
            key.Length.ShouldBe(32); // AES-256 = 32 bytes
        });
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
        first.IfRight(k1 => second.IfRight(k2 => k1.ShouldBe(k2)));
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
        created.IfRight(k1 => result.IfRight(k2 => k1.ShouldBe(k2)));
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
}
