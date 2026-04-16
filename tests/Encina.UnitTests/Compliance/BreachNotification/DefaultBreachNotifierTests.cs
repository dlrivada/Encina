#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Model;

using Shouldly;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.Compliance.BreachNotification;

/// <summary>
/// Unit tests for <see cref="DefaultBreachNotifier"/>.
/// </summary>
public class DefaultBreachNotifierTests
{
    private static BreachRecord CreateTestBreach(string? id = null)
    {
        return BreachRecord.Create(
            nature: "Unauthorized access to personal data",
            approximateSubjectsAffected: 500,
            categoriesOfDataAffected: ["email", "name"],
            dpoContactDetails: "dpo@example.com",
            likelyConsequences: "Identity theft risk",
            measuresTaken: "Access revoked, passwords reset",
            detectedAtUtc: DateTimeOffset.UtcNow,
            severity: BreachSeverity.High);
    }

    private static DefaultBreachNotifier CreateSut(
        FakeTimeProvider? timeProvider = null)
    {
        return new DefaultBreachNotifier(
            timeProvider ?? new FakeTimeProvider(),
            NullLogger<DefaultBreachNotifier>.Instance);
    }

    #region NotifyAuthorityAsync Tests

    [Fact]
    public async Task NotifyAuthorityAsync_ReturnsNotificationResultSent()
    {
        // Arrange
        var sut = CreateSut();
        var breach = CreateTestBreach();

        // Act
        var result = await sut.NotifyAuthorityAsync(breach);

        // Assert
        result.IsRight.ShouldBeTrue();
        var notification = result.Match(r => r, _ => null!);
        notification.Outcome.ShouldBe(NotificationOutcome.Sent);
    }

    [Fact]
    public async Task NotifyAuthorityAsync_SetsCorrectBreachId()
    {
        // Arrange
        var sut = CreateSut();
        var breach = CreateTestBreach();

        // Act
        var result = await sut.NotifyAuthorityAsync(breach);

        // Assert
        result.IsRight.ShouldBeTrue();
        var notification = result.Match(r => r, _ => null!);
        notification.BreachId.ShouldBe(breach.Id);
    }

    [Fact]
    public async Task NotifyAuthorityAsync_UsesTimeProviderForSentTimestamp()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var expectedTime = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
        timeProvider.SetUtcNow(expectedTime);
        var sut = CreateSut(timeProvider);
        var breach = CreateTestBreach();

        // Act
        var result = await sut.NotifyAuthorityAsync(breach);

        // Assert
        result.IsRight.ShouldBeTrue();
        var notification = result.Match(r => r, _ => null!);
        notification.SentAtUtc.ShouldBe(expectedTime);
    }

    [Fact]
    public async Task NotifyAuthorityAsync_SetsRecipientToSupervisoryAuthority()
    {
        // Arrange
        var sut = CreateSut();
        var breach = CreateTestBreach();

        // Act
        var result = await sut.NotifyAuthorityAsync(breach);

        // Assert
        result.IsRight.ShouldBeTrue();
        var notification = result.Match(r => r, _ => null!);
        notification.Recipient!.ShouldContain("supervisory-authority");
    }

    #endregion

    #region NotifyDataSubjectsAsync Tests

    [Fact]
    public async Task NotifyDataSubjectsAsync_ReturnsNotificationResultSent()
    {
        // Arrange
        var sut = CreateSut();
        var breach = CreateTestBreach();
        var subjectIds = new[] { "subject-1", "subject-2", "subject-3" };

        // Act
        var result = await sut.NotifyDataSubjectsAsync(breach, subjectIds);

        // Assert
        result.IsRight.ShouldBeTrue();
        var notification = result.Match(r => r, _ => null!);
        notification.Outcome.ShouldBe(NotificationOutcome.Sent);
    }

    [Fact]
    public async Task NotifyDataSubjectsAsync_SetsCorrectBreachId()
    {
        // Arrange
        var sut = CreateSut();
        var breach = CreateTestBreach();
        var subjectIds = new[] { "subject-1" };

        // Act
        var result = await sut.NotifyDataSubjectsAsync(breach, subjectIds);

        // Assert
        result.IsRight.ShouldBeTrue();
        var notification = result.Match(r => r, _ => null!);
        notification.BreachId.ShouldBe(breach.Id);
    }

    [Fact]
    public async Task NotifyDataSubjectsAsync_IncludesSubjectCountInRecipient()
    {
        // Arrange
        var sut = CreateSut();
        var breach = CreateTestBreach();
        var subjectIds = new[] { "subject-1", "subject-2", "subject-3" };

        // Act
        var result = await sut.NotifyDataSubjectsAsync(breach, subjectIds);

        // Assert
        result.IsRight.ShouldBeTrue();
        var notification = result.Match(r => r, _ => null!);
        notification.Recipient!.ShouldContain("3 subjects");
    }

    [Fact]
    public async Task NotifyDataSubjectsAsync_UsesTimeProviderForSentTimestamp()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var expectedTime = new DateTimeOffset(2026, 3, 1, 14, 30, 0, TimeSpan.Zero);
        timeProvider.SetUtcNow(expectedTime);
        var sut = CreateSut(timeProvider);
        var breach = CreateTestBreach();
        var subjectIds = new[] { "subject-1" };

        // Act
        var result = await sut.NotifyDataSubjectsAsync(breach, subjectIds);

        // Assert
        result.IsRight.ShouldBeTrue();
        var notification = result.Match(r => r, _ => null!);
        notification.SentAtUtc.ShouldBe(expectedTime);
    }

    #endregion
}
