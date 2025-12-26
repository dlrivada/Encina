namespace Encina.Marten.Versioning;

/// <summary>
/// Error codes for event versioning operations.
/// </summary>
public static class EventVersioningErrorCodes
{
    /// <summary>
    /// Error code prefix for all event versioning errors.
    /// </summary>
    private const string Prefix = "event.versioning.";

    /// <summary>
    /// Event upcasting failed.
    /// </summary>
    public const string UpcastFailed = $"{Prefix}upcast_failed";

    /// <summary>
    /// No upcaster found for the event type.
    /// </summary>
    public const string UpcasterNotFound = $"{Prefix}upcaster_not_found";

    /// <summary>
    /// Upcaster registration failed.
    /// </summary>
    public const string RegistrationFailed = $"{Prefix}registration_failed";

    /// <summary>
    /// Duplicate upcaster registration for the same source event type.
    /// </summary>
    public const string DuplicateUpcaster = $"{Prefix}duplicate_upcaster";

    /// <summary>
    /// Invalid upcaster configuration.
    /// </summary>
    public const string InvalidConfiguration = $"{Prefix}invalid_configuration";
}
