namespace SimpleMediator.MongoDB;

/// <summary>
/// Error codes for MongoDB operations in SimpleMediator.
/// </summary>
public static class MongoDbErrorCodes
{
    /// <summary>
    /// Error code for connection failures.
    /// </summary>
    public const string ConnectionFailed = "MONGODB_CONNECTION_FAILED";

    /// <summary>
    /// Error code for document not found.
    /// </summary>
    public const string DocumentNotFound = "MONGODB_DOCUMENT_NOT_FOUND";

    /// <summary>
    /// Error code for duplicate key violations.
    /// </summary>
    public const string DuplicateKey = "MONGODB_DUPLICATE_KEY";

    /// <summary>
    /// Error code for write conflicts (optimistic concurrency).
    /// </summary>
    public const string WriteConflict = "MONGODB_WRITE_CONFLICT";

    /// <summary>
    /// Error code for serialization failures.
    /// </summary>
    public const string SerializationFailed = "MONGODB_SERIALIZATION_FAILED";

    /// <summary>
    /// Error code for timeout errors.
    /// </summary>
    public const string Timeout = "MONGODB_TIMEOUT";

    /// <summary>
    /// Error code for general MongoDB errors.
    /// </summary>
    public const string GeneralError = "MONGODB_ERROR";
}
