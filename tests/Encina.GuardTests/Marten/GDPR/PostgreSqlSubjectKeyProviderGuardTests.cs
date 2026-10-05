using Encina.Marten.GDPR;

using Marten;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Encina.GuardTests.Marten.GDPR;

/// <summary>
/// Guard clause tests for <see cref="PostgreSqlSubjectKeyProvider"/>: constructor arguments and the subject id of
/// every public method (#1699). The subject id is checked before any database access.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "Marten")]
public sealed class PostgreSqlSubjectKeyProviderGuardTests
{
    [Fact]
    public void Constructor_NullSession_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            new PostgreSqlSubjectKeyProvider(null!, TimeProvider.System, NullLogger<PostgreSqlSubjectKeyProvider>.Instance));
        ex.ParamName.ShouldBe("session");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            new PostgreSqlSubjectKeyProvider(Session(), null!, NullLogger<PostgreSqlSubjectKeyProvider>.Instance));
        ex.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            new PostgreSqlSubjectKeyProvider(Session(), TimeProvider.System, null!));
        ex.ParamName.ShouldBe("logger");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetOrCreateSubjectKeyAsync_InvalidSubjectId_ThrowsArgumentException(string? subjectId)
    {
        var ex = await Should.ThrowAsync<ArgumentException>(() => Sut().GetOrCreateSubjectKeyAsync(subjectId!).AsTask());
        ex.ParamName.ShouldBe("subjectId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSubjectKeyAsync_InvalidSubjectId_ThrowsArgumentException(string? subjectId)
    {
        var ex = await Should.ThrowAsync<ArgumentException>(() => Sut().GetSubjectKeyAsync(subjectId!).AsTask());
        ex.ParamName.ShouldBe("subjectId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteSubjectKeysAsync_InvalidSubjectId_ThrowsArgumentException(string? subjectId)
    {
        var ex = await Should.ThrowAsync<ArgumentException>(() => Sut().DeleteSubjectKeysAsync(subjectId!).AsTask());
        ex.ParamName.ShouldBe("subjectId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IsSubjectForgottenAsync_InvalidSubjectId_ThrowsArgumentException(string? subjectId)
    {
        var ex = await Should.ThrowAsync<ArgumentException>(() => Sut().IsSubjectForgottenAsync(subjectId!).AsTask());
        ex.ParamName.ShouldBe("subjectId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RotateSubjectKeyAsync_InvalidSubjectId_ThrowsArgumentException(string? subjectId)
    {
        var ex = await Should.ThrowAsync<ArgumentException>(() => Sut().RotateSubjectKeyAsync(subjectId!).AsTask());
        ex.ParamName.ShouldBe("subjectId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSubjectInfoAsync_InvalidSubjectId_ThrowsArgumentException(string? subjectId)
    {
        var ex = await Should.ThrowAsync<ArgumentException>(() => Sut().GetSubjectInfoAsync(subjectId!).AsTask());
        ex.ParamName.ShouldBe("subjectId");
    }

    private static IDocumentSession Session()
    {
        var session = Substitute.For<IDocumentSession>();
        session.DocumentStore.Returns(Substitute.For<IDocumentStore>());
        session.TenantId.Returns("*DEFAULT*");
        return session;
    }

    private static PostgreSqlSubjectKeyProvider Sut() =>
        new(Session(), TimeProvider.System, NullLogger<PostgreSqlSubjectKeyProvider>.Instance);
}
