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

    [Fact]
    public async Task LocateAllDataAsync_SessionFailure_ReturnsErrorWithoutLoggingSubjectId()
    {
        // _mockSession.Events is unconfigured (null), so querying it throws — exercising the
        // fail-closed catch path without needing to mock Marten's full query pipeline.
        var logger = new Microsoft.Extensions.Logging.Testing.FakeLogger<MartenEventPersonalDataLocator>();
        var sut = new MartenEventPersonalDataLocator(_mockSession, logger);

        var result = await sut.LocateAllDataAsync("subject-1");

        result.IsLeft.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldAllBe(r => !r.Message.Contains("subject-1", StringComparison.Ordinal));
    }

    // Note: Behavioral tests of the full LocateAllDataAsync (actual event-store scanning) are
    // covered by integration tests, because mocking the deep Marten event store pipeline
    // (IEventStore, IMartenQueryable) is fragile and version-dependent. The per-event field
    // matching logic itself — the part that assigns PersonalDataLocation.EntityId = subjectId,
    // relevant to #1429's leak — is pure (no Marten dependency) and is exercised directly below.

    #region LocateFieldsInEvent / TryBuildLocation

    [Fact]
    public void LocateFieldsInEvent_MatchingSubject_ReturnsLocation()
    {
        var evt = new PiiEvent { UserId = "subject-1", Email = "test@example.com" };

        var locations = MartenEventPersonalDataLocator.LocateFieldsInEvent(evt, "subject-1").ToList();

        locations.Count.ShouldBe(1);
        locations[0].EntityId.ShouldBe("subject-1");
        locations[0].FieldName.ShouldBe(nameof(PiiEvent.Email));
        locations[0].EntityType.ShouldBe(typeof(PiiEvent));
        locations[0].CurrentValue.ShouldBe("test@example.com");
    }

    [Fact]
    public void LocateFieldsInEvent_DifferentSubject_ReturnsEmpty()
    {
        var evt = new PiiEvent { UserId = "subject-1", Email = "test@example.com" };

        var locations = MartenEventPersonalDataLocator.LocateFieldsInEvent(evt, "subject-2").ToList();

        locations.ShouldBeEmpty();
    }

    [Fact]
    public void LocateFieldsInEvent_NoCryptoShreddedFields_ReturnsEmpty()
    {
        var evt = new NonPiiEvent { Id = "123" };

        var locations = MartenEventPersonalDataLocator.LocateFieldsInEvent(evt, "subject-1").ToList();

        locations.ShouldBeEmpty();
    }

    [Fact]
    public void TryBuildLocation_UnresolvableSubjectIdProperty_ReturnsFalse()
    {
        // CryptoShreddedPropertyCache.GetFields excludes a field whose SubjectIdProperty does
        // not resolve, so TryBuildLocation's own null-property guard is unreachable through the
        // cache — exercised directly here instead, by handing it a field descriptor built from
        // the cache's own valid field but retargeted to a nonexistent SubjectIdProperty name.
        var evt = new PiiEvent { UserId = "subject-1", Email = "test@example.com" };
        var validField = CryptoShreddedPropertyCache.GetFields(typeof(PiiEvent)).Single();
        var fieldWithBadSubjectIdProperty = new CryptoShreddedFieldInfo(
            validField.Property, validField.Attribute, validField.Setter, "DoesNotExist");

        var found = MartenEventPersonalDataLocator.TryBuildLocation(
            evt, typeof(PiiEvent), fieldWithBadSubjectIdProperty, "subject-1", out _);

        found.ShouldBeFalse();
    }

    [Fact]
    public void TryBuildLocation_SubjectMismatch_ReturnsFalse()
    {
        var evt = new PiiEvent { UserId = "subject-1", Email = "test@example.com" };
        var field = CryptoShreddedPropertyCache.GetFields(typeof(PiiEvent)).Single();

        var found = MartenEventPersonalDataLocator.TryBuildLocation(
            evt, typeof(PiiEvent), field, "subject-2", out _);

        found.ShouldBeFalse();
    }

    public class PiiEvent
    {
        public string UserId { get; set; } = string.Empty;

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;
    }

    public class NonPiiEvent
    {
        public string Id { get; set; } = string.Empty;
    }

    #endregion
}
