namespace Encina.Cdc.DeadLetter;

/// <summary>
/// Specifies how a dead-lettered CDC event should be resolved.
/// </summary>
public enum CdcDeadLetterResolution
{
    /// <summary>Replay the event for reprocessing.</summary>
    Replay = 0,

    /// <summary>Discard the event permanently.</summary>
    Discard = 1
}
