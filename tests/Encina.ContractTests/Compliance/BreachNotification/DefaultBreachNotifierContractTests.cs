#pragma warning disable CA2012

using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Model;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.ContractTests.Compliance.BreachNotification;

/// <summary>
/// Contract tests verifying <see cref="DefaultBreachNotifier"/> behavioral contract:
/// authority notification returns Sent, subject notification returns Sent.
/// </summary>
[Trait("Category", "Contract")]
public class DefaultBreachNotifierContractTests
{
    private readonly DefaultBreachNotifier _notifier = new(
        TimeProvider.System,
        NullLogger<DefaultBreachNotifier>.Instance);

    [Fact]
    public async Task Contract_NotifyAuthorityAsync_ReturnsSuccess()
    {
        var breach = BreachRecord.Create(
            nature: "unauthorized access",
            approximateSubjectsAffected: 100,
            categoriesOfDataAffected: ["email", "name"],
            dpoContactDetails: "dpo@example.com",
            likelyConsequences: "Identity theft risk",
            measuresTaken: "Access revoked, investigation started",
            detectedAtUtc: DateTimeOffset.UtcNow,
            severity: BreachSeverity.High);

        var result = await _notifier.NotifyAuthorityAsync(breach);

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.Outcome.ShouldBe(NotificationOutcome.Sent),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task Contract_NotifyDataSubjectsAsync_ReturnsSuccess()
    {
        var breach = BreachRecord.Create(
            nature: "data exfiltration",
            approximateSubjectsAffected: 50,
            categoriesOfDataAffected: ["health"],
            dpoContactDetails: "dpo@example.com",
            likelyConsequences: "Health data exposure",
            measuresTaken: "Encryption applied retroactively",
            detectedAtUtc: DateTimeOffset.UtcNow,
            severity: BreachSeverity.Critical);

        var result = await _notifier.NotifyDataSubjectsAsync(breach, ["subject-1", "subject-2"]);

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r =>
            {
                r.Outcome.ShouldBe(NotificationOutcome.Sent);
                r.Recipient!.ShouldContain("2 subjects");
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }
}
