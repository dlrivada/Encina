using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten.GDPR;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.Marten.GDPR;

/// <summary>
/// Integration tests for <see cref="PostgreSqlSubjectKeyProvider"/> using a real PostgreSQL instance.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class PostgreSqlSubjectKeyProviderIntegrationTests
{
    private readonly MartenFixture _fixture;

    public PostgreSqlSubjectKeyProviderIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetOrCreateSubjectKeyAsync_NewSubject_CreatesKey()
    {
        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var sut = new PostgreSqlSubjectKeyProvider(
            session, TimeProvider.System, NullLogger<PostgreSqlSubjectKeyProvider>.Instance);
        var subjectId = $"integration-test-{Guid.NewGuid():N}";

        // Act
        var result = await sut.GetOrCreateSubjectKeyAsync(subjectId);

        // Assert
        result.IsRight.ShouldBeTrue("Should create key for new subject");
        result.IfRight(key => key.Length.ShouldBe(32, "AES-256 key should be 32 bytes"));
    }

    [Fact]
    public async Task GetOrCreateSubjectKeyAsync_ExistingSubject_ReturnsSameKey()
    {
        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var sut = new PostgreSqlSubjectKeyProvider(
            session, TimeProvider.System, NullLogger<PostgreSqlSubjectKeyProvider>.Instance);
        var subjectId = $"integration-test-{Guid.NewGuid():N}";

        // Act
        var first = await sut.GetOrCreateSubjectKeyAsync(subjectId);
        var second = await sut.GetOrCreateSubjectKeyAsync(subjectId);

        // Assert
        first.IsRight.ShouldBeTrue();
        second.IsRight.ShouldBeTrue();

        byte[] firstKey = null!;
        byte[] secondKey = null!;
        first.IfRight(k => firstKey = k);
        second.IfRight(k => secondKey = k);

        firstKey.SequenceEqual(secondKey).ShouldBeTrue(
            "Getting the same subject twice should return the same key");
    }

    [Fact]
    public async Task GetSubjectKeyAsync_ExistingSubject_ReturnsKey()
    {
        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var sut = new PostgreSqlSubjectKeyProvider(
            session, TimeProvider.System, NullLogger<PostgreSqlSubjectKeyProvider>.Instance);
        var subjectId = $"integration-test-{Guid.NewGuid():N}";

        var createResult = await sut.GetOrCreateSubjectKeyAsync(subjectId);
        createResult.IsRight.ShouldBeTrue();

        // Act
        var getResult = await sut.GetSubjectKeyAsync(subjectId);

        // Assert
        getResult.IsRight.ShouldBeTrue("Should retrieve existing key");

        byte[] createdKey = null!;
        byte[] gottenKey = null!;
        createResult.IfRight(k => createdKey = k);
        getResult.IfRight(k => gottenKey = k);

        createdKey.SequenceEqual(gottenKey).ShouldBeTrue();
    }

    [Fact]
    public async Task GetSubjectKeyAsync_NonExistentSubject_ReturnsError()
    {
        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var sut = new PostgreSqlSubjectKeyProvider(
            session, TimeProvider.System, NullLogger<PostgreSqlSubjectKeyProvider>.Instance);

        // Act
        var result = await sut.GetSubjectKeyAsync($"nonexistent-{Guid.NewGuid():N}");

        // Assert
        result.IsLeft.ShouldBeTrue("Should return error for non-existent subject");
    }

    [Fact]
    public async Task DeleteSubjectKeysAsync_ForgetsSubject()
    {
        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var sut = new PostgreSqlSubjectKeyProvider(
            session, TimeProvider.System, NullLogger<PostgreSqlSubjectKeyProvider>.Instance);
        var subjectId = $"integration-test-{Guid.NewGuid():N}";

        await sut.GetOrCreateSubjectKeyAsync(subjectId);

        // Act
        var deleteResult = await sut.DeleteSubjectKeysAsync(subjectId);

        // Assert
        deleteResult.IsRight.ShouldBeTrue("Delete should succeed");
        deleteResult.IfRight(r =>
        {
            r.SubjectId.ShouldBe(subjectId);
            r.KeysDeleted.ShouldBeGreaterThanOrEqualTo(1);
        });

        var forgottenResult = await sut.IsSubjectForgottenAsync(subjectId);
        forgottenResult.IsRight.ShouldBeTrue();
        forgottenResult.IfRight(f => f.ShouldBeTrue("Subject should be forgotten after delete"));
    }

    [Fact]
    public async Task DeleteSubjectKeysAsync_ForgottenSubject_GetKeyReturnsError()
    {
        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var sut = new PostgreSqlSubjectKeyProvider(
            session, TimeProvider.System, NullLogger<PostgreSqlSubjectKeyProvider>.Instance);
        var subjectId = $"integration-test-{Guid.NewGuid():N}";

        await sut.GetOrCreateSubjectKeyAsync(subjectId);
        await sut.DeleteSubjectKeysAsync(subjectId);

        // Act
        var getResult = await sut.GetSubjectKeyAsync(subjectId);

        // Assert
        getResult.IsLeft.ShouldBeTrue("Get key for forgotten subject should return error");
    }

    [Fact]
    public async Task RotateSubjectKeyAsync_ProducesNewVersion()
    {
        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var sut = new PostgreSqlSubjectKeyProvider(
            session, TimeProvider.System, NullLogger<PostgreSqlSubjectKeyProvider>.Instance);
        var subjectId = $"integration-test-{Guid.NewGuid():N}";

        var originalResult = await sut.GetOrCreateSubjectKeyAsync(subjectId);
        originalResult.IsRight.ShouldBeTrue();

        byte[] originalKey = null!;
        originalResult.IfRight(k => originalKey = k);

        // Act
        var rotateResult = await sut.RotateSubjectKeyAsync(subjectId);

        // Assert
        rotateResult.IsRight.ShouldBeTrue("Rotation should succeed");
        rotateResult.IfRight(r =>
        {
            r.SubjectId.ShouldBe(subjectId);
            r.NewVersion.ShouldBe(2);
        });

        var newKeyResult = await sut.GetSubjectKeyAsync(subjectId);
        newKeyResult.IsRight.ShouldBeTrue();

        byte[] newKey = null!;
        newKeyResult.IfRight(k => newKey = k);

        originalKey.SequenceEqual(newKey).ShouldBeFalse("Rotated key should differ from original");
    }

    [Fact]
    public async Task GetSubjectInfoAsync_ActiveSubject_ReturnsCorrectInfo()
    {
        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var sut = new PostgreSqlSubjectKeyProvider(
            session, TimeProvider.System, NullLogger<PostgreSqlSubjectKeyProvider>.Instance);
        var subjectId = $"integration-test-{Guid.NewGuid():N}";

        await sut.GetOrCreateSubjectKeyAsync(subjectId);

        // Act
        var infoResult = await sut.GetSubjectInfoAsync(subjectId);

        // Assert
        infoResult.IsRight.ShouldBeTrue();
        infoResult.IfRight(info =>
        {
            info.SubjectId.ShouldBe(subjectId);
            Assert.Equal(SubjectStatus.Active, info.Status);
            info.ActiveKeyVersion.ShouldBe(1);
            info.TotalKeyVersions.ShouldBe(1);
        });
    }

    [Fact]
    public async Task GetSubjectInfoAsync_ForgottenSubject_ReturnsCorrectInfo()
    {
        // Arrange
        await using var session = _fixture.Store!.LightweightSession();
        var sut = new PostgreSqlSubjectKeyProvider(
            session, TimeProvider.System, NullLogger<PostgreSqlSubjectKeyProvider>.Instance);
        var subjectId = $"integration-test-{Guid.NewGuid():N}";

        await sut.GetOrCreateSubjectKeyAsync(subjectId);
        await sut.DeleteSubjectKeysAsync(subjectId);

        // Act
        var infoResult = await sut.GetSubjectInfoAsync(subjectId);

        // Assert
        infoResult.IsRight.ShouldBeTrue();
        infoResult.IfRight(info =>
        {
            info.SubjectId.ShouldBe(subjectId);
            Assert.Equal(SubjectStatus.Forgotten, info.Status);
            info.ForgottenAtUtc.ShouldNotBeNull();
        });
    }
}
