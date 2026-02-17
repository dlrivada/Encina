namespace Encina.Cdc.DeadLetter;

/// <summary>
/// Represents the current status of a dead-lettered CDC event.
/// </summary>
public enum CdcDeadLetterStatus
{
    /// <summary>The entry is pending review or replay.</summary>
    Pending = 0,

    /// <summary>The entry has been successfully replayed.</summary>
    Replayed = 1,

    /// <summary>The entry has been discarded by an operator.</summary>
    Discarded = 2
}
