using Encina.Marten.GDPR;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Encina.GuardTests.Marten.GDPR;

/// <summary>
/// Guard clause tests for <see cref="InMemorySubjectKeyProvider"/>.
/// Verifies null/empty/whitespace checks on constructor parameters and public methods.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "Marten")]
public sealed class InMemorySubjectKeyProviderGuardTests
{
    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new InMemorySubjectKeyProvider(null!, NullLogger<InMemorySubjectKeyProvider>.Instance));
        ex.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new InMemorySubjectKeyProvider(TimeProvider.System, null!));
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task GetOrCreateSubjectKeyAsync_NullSubjectId_ThrowsArgumentException()
    {
        // Arrange
        var sut = new InMemorySubjectKeyProvider(TimeProvider.System, NullLogger<InMemorySubjectKeyProvider>.Instance);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            sut.GetOrCreateSubjectKeyAsync(null!).AsTask());
        ex.ParamName.ShouldBe("subjectId");
    }

    [Fact]
    public async Task GetOrCreateSubjectKeyAsync_EmptySubjectId_ThrowsArgumentException()
    {
        // Arrange
        var sut = new InMemorySubjectKeyProvider(TimeProvider.System, NullLogger<InMemorySubjectKeyProvider>.Instance);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            sut.GetOrCreateSubjectKeyAsync("").AsTask());
        ex.ParamName.ShouldBe("subjectId");
    }

    [Fact]
    public async Task GetOrCreateSubjectKeyAsync_WhitespaceSubjectId_ThrowsArgumentException()
    {
        // Arrange
        var sut = new InMemorySubjectKeyProvider(TimeProvider.System, NullLogger<InMemorySubjectKeyProvider>.Instance);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            sut.GetOrCreateSubjectKeyAsync("   ").AsTask());
        ex.ParamName.ShouldBe("subjectId");
    }

    [Fact]
    public async Task GetSubjectKeyAsync_NullSubjectId_ThrowsArgumentException()
    {
        // Arrange
        var sut = new InMemorySubjectKeyProvider(TimeProvider.System, NullLogger<InMemorySubjectKeyProvider>.Instance);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            sut.GetSubjectKeyAsync(null!).AsTask());
        ex.ParamName.ShouldBe("subjectId");
    }

    [Fact]
    public async Task GetSubjectKeyAsync_EmptySubjectId_ThrowsArgumentException()
    {
        // Arrange
        var sut = new InMemorySubjectKeyProvider(TimeProvider.System, NullLogger<InMemorySubjectKeyProvider>.Instance);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            sut.GetSubjectKeyAsync("").AsTask());
        ex.ParamName.ShouldBe("subjectId");
    }

    [Fact]
    public async Task DeleteSubjectKeysAsync_NullSubjectId_ThrowsArgumentException()
    {
        // Arrange
        var sut = new InMemorySubjectKeyProvider(TimeProvider.System, NullLogger<InMemorySubjectKeyProvider>.Instance);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            sut.DeleteSubjectKeysAsync(null!).AsTask());
        ex.ParamName.ShouldBe("subjectId");
    }

    [Fact]
    public async Task DeleteSubjectKeysAsync_EmptySubjectId_ThrowsArgumentException()
    {
        // Arrange
        var sut = new InMemorySubjectKeyProvider(TimeProvider.System, NullLogger<InMemorySubjectKeyProvider>.Instance);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            sut.DeleteSubjectKeysAsync("").AsTask());
        ex.ParamName.ShouldBe("subjectId");
    }

    [Fact]
    public async Task IsSubjectForgottenAsync_NullSubjectId_ThrowsArgumentException()
    {
        // Arrange
        var sut = new InMemorySubjectKeyProvider(TimeProvider.System, NullLogger<InMemorySubjectKeyProvider>.Instance);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            sut.IsSubjectForgottenAsync(null!).AsTask());
        ex.ParamName.ShouldBe("subjectId");
    }

    [Fact]
    public async Task RotateSubjectKeyAsync_NullSubjectId_ThrowsArgumentException()
    {
        // Arrange
        var sut = new InMemorySubjectKeyProvider(TimeProvider.System, NullLogger<InMemorySubjectKeyProvider>.Instance);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            sut.RotateSubjectKeyAsync(null!).AsTask());
        ex.ParamName.ShouldBe("subjectId");
    }

    [Fact]
    public async Task RotateSubjectKeyAsync_EmptySubjectId_ThrowsArgumentException()
    {
        // Arrange
        var sut = new InMemorySubjectKeyProvider(TimeProvider.System, NullLogger<InMemorySubjectKeyProvider>.Instance);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            sut.RotateSubjectKeyAsync("").AsTask());
        ex.ParamName.ShouldBe("subjectId");
    }

    [Fact]
    public async Task GetSubjectInfoAsync_NullSubjectId_ThrowsArgumentException()
    {
        // Arrange
        var sut = new InMemorySubjectKeyProvider(TimeProvider.System, NullLogger<InMemorySubjectKeyProvider>.Instance);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            sut.GetSubjectInfoAsync(null!).AsTask());
        ex.ParamName.ShouldBe("subjectId");
    }

    [Fact]
    public async Task GetSubjectInfoAsync_EmptySubjectId_ThrowsArgumentException()
    {
        // Arrange
        var sut = new InMemorySubjectKeyProvider(TimeProvider.System, NullLogger<InMemorySubjectKeyProvider>.Instance);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            sut.GetSubjectInfoAsync("").AsTask());
        ex.ParamName.ShouldBe("subjectId");
    }
}
