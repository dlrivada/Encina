using Encina.Compliance.BreachNotification.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.BreachNotification;

/// <summary>
/// Property-based tests for <see cref="SecurityEvent"/> verifying domain invariants
/// using FsCheck random data generation.
/// </summary>
public class SecurityEventPropertyTests
{
    [Property(MaxTest = 50)]
    public bool Create_AlwaysGenerates32CharHexId(NonEmptyString source, NonEmptyString description)
    {
        var evt = SecurityEvent.Create(
            SecurityEventType.UnauthorizedAccess,
            source.Get,
            description.Get,
            DateTimeOffset.UtcNow);

        return evt.Id.Length == 32
            && evt.Id.All(c => char.IsAsciiHexDigitLower(c) || char.IsAsciiDigit(c));
    }

    [Property(MaxTest = 50)]
    public bool Create_PreservesEventType(NonEmptyString source, NonEmptyString desc)
    {
        var evt = SecurityEvent.Create(
            SecurityEventType.DataExfiltration,
            source.Get,
            desc.Get,
            DateTimeOffset.UtcNow);

        return evt.EventType == SecurityEventType.DataExfiltration;
    }

    [Property(MaxTest = 50)]
    public bool Create_PreservesSource(NonEmptyString source, NonEmptyString desc)
    {
        var evt = SecurityEvent.Create(
            SecurityEventType.UnauthorizedAccess,
            source.Get,
            desc.Get,
            DateTimeOffset.UtcNow);

        return evt.Source == source.Get;
    }

    [Property(MaxTest = 50)]
    public bool Create_PreservesDescription(NonEmptyString source, NonEmptyString desc)
    {
        var evt = SecurityEvent.Create(
            SecurityEventType.AnomalousQuery,
            source.Get,
            desc.Get,
            DateTimeOffset.UtcNow);

        return evt.Description == desc.Get;
    }

    [Property(MaxTest = 50)]
    public bool Create_PreservesOccurredAtUtc(NonEmptyString source, NonEmptyString desc)
    {
        var timestamp = DateTimeOffset.UtcNow;

        var evt = SecurityEvent.Create(
            SecurityEventType.PrivilegeEscalation,
            source.Get,
            desc.Get,
            timestamp);

        return evt.OccurredAtUtc == timestamp;
    }
}
