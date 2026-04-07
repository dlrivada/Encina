using Encina.Compliance.BreachNotification.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.BreachNotification;

/// <summary>
/// Property-based tests for <see cref="NotificationResult"/> verifying domain invariants
/// using FsCheck random data generation.
/// </summary>
public class NotificationResultPropertyTests
{
    [Property(MaxTest = 50)]
    public bool Outcome_IsPreserved(NonEmptyString breachId)
    {
        var result = new NotificationResult
        {
            Outcome = NotificationOutcome.Sent,
            BreachId = breachId.Get
        };

        return result.Outcome == NotificationOutcome.Sent;
    }

    [Property(MaxTest = 50)]
    public bool BreachId_IsPreserved(NonEmptyString breachId)
    {
        var result = new NotificationResult
        {
            Outcome = NotificationOutcome.Pending,
            BreachId = breachId.Get
        };

        return result.BreachId == breachId.Get;
    }

    [Property(MaxTest = 50)]
    public bool SentAtUtc_NullWhenNotSent(NonEmptyString breachId)
    {
        var result = new NotificationResult
        {
            Outcome = NotificationOutcome.Failed,
            BreachId = breachId.Get,
            ErrorMessage = "Delivery failed"
        };

        return result.SentAtUtc is null;
    }

    [Property(MaxTest = 50)]
    public bool Recipient_NullByDefault(NonEmptyString breachId)
    {
        var result = new NotificationResult
        {
            Outcome = NotificationOutcome.Pending,
            BreachId = breachId.Get
        };

        return result.Recipient is null;
    }

    [Property(MaxTest = 50)]
    public bool ErrorMessage_NullWhenSent(NonEmptyString breachId)
    {
        var result = new NotificationResult
        {
            Outcome = NotificationOutcome.Sent,
            SentAtUtc = DateTimeOffset.UtcNow,
            BreachId = breachId.Get
        };

        return result.ErrorMessage is null;
    }
}
