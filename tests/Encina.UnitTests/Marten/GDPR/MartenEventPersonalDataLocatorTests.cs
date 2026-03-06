using Encina.Compliance.DataSubjectRights;
using Encina.Marten.GDPR;

using Marten;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Encina.UnitTests.Marten.GDPR;

public sealed class MartenEventPersonalDataLocatorTests : IDisposable
{
    private readonly IDocumentSession _mockSession = Substitute.For<IDocumentSession>();

    public void Dispose()
    {
        CryptoShreddedPropertyCache.ClearCache();
    }

    [Fact]
    public void Constructor_NullSession_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new MartenEventPersonalDataLocator(
                null!,
                NullLogger<MartenEventPersonalDataLocator>.Instance));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new MartenEventPersonalDataLocator(
                _mockSession,
                null!));
    }

    [Fact]
    public void ImplementsIPersonalDataLocator()
    {
        // Arrange
        var sut = new MartenEventPersonalDataLocator(
            _mockSession,
            NullLogger<MartenEventPersonalDataLocator>.Instance);

        // Assert
        sut.ShouldBeAssignableTo<IPersonalDataLocator>();
    }

    [Fact]
    public async Task LocateAllDataAsync_NullSubjectId_ThrowsArgumentException()
    {
        // Arrange
        var sut = new MartenEventPersonalDataLocator(
            _mockSession,
            NullLogger<MartenEventPersonalDataLocator>.Instance);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => sut.LocateAllDataAsync(null!).AsTask());
    }

    [Fact]
    public async Task LocateAllDataAsync_EmptySubjectId_ThrowsArgumentException()
    {
        // Arrange
        var sut = new MartenEventPersonalDataLocator(
            _mockSession,
            NullLogger<MartenEventPersonalDataLocator>.Instance);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => sut.LocateAllDataAsync("").AsTask());
    }

    [Fact]
    public async Task LocateAllDataAsync_WhitespaceSubjectId_ThrowsArgumentException()
    {
        // Arrange
        var sut = new MartenEventPersonalDataLocator(
            _mockSession,
            NullLogger<MartenEventPersonalDataLocator>.Instance);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => sut.LocateAllDataAsync("   ").AsTask());
    }

    // Note: Behavioral tests (actual event scanning) are covered by integration tests
    // in CryptoShredderSerializerIntegrationTests and ForgetSubjectIntegrationTests,
    // because mocking the deep Marten event store pipeline (IEventStore, IMartenQueryable)
    // is fragile and version-dependent.
}
