namespace Encina;

/// <summary>
/// The protocol-neutral kind of an <see cref="EncinaError"/>, set where the error is created and read with
/// <see cref="EncinaErrorExtensions.GetCategory(EncinaError)"/>.
/// </summary>
/// <remarks>
/// <para>
/// Each protocol owns a table from category to its own status: HTTP in the hosts, gRPC in <c>Encina.gRPC</c>
/// and GraphQL in <c>Encina.GraphQL</c> (ADR-062). The producer never knows the protocol; the host never parses
/// the error code.
/// </para>
/// <para>
/// The values are explicit and stable, and the set is additive only: a value is never renumbered or removed.
/// <see cref="Internal"/> is <c>0</c>, so a default or unset value fails toward a server error. Adding a category
/// means adding a row to every protocol table and to the property tests.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// return EncinaErrors.Create(OrderErrorCodes.NotFound, ErrorCategory.NotFound, $"Order {id} was not found.");
/// </code>
/// </example>
public enum ErrorCategory
{
    /// <summary>
    /// A server fault: a bug, a wiring or configuration fault, a store, serialization or crypto failure, a null
    /// request or a missing handler. HTTP 500, gRPC <c>INTERNAL</c>.
    /// </summary>
    Internal = 0,

    /// <summary>
    /// The caller's input broke validation rules (field errors). HTTP 400, gRPC <c>INVALID_ARGUMENT</c>.
    /// </summary>
    Validation = 1,

    /// <summary>
    /// The request is malformed or lacks a required element (a header, an idempotency key, a message id or a
    /// tenant). HTTP 400, gRPC <c>INVALID_ARGUMENT</c>.
    /// </summary>
    InvalidRequest = 2,

    /// <summary>
    /// The request has no credentials or invalid ones. HTTP 401 with <c>WWW-Authenticate</c>, gRPC
    /// <c>UNAUTHENTICATED</c>.
    /// </summary>
    Unauthenticated = 3,

    /// <summary>
    /// An authenticated caller is denied. HTTP 403, gRPC <c>PERMISSION_DENIED</c>.
    /// </summary>
    Forbidden = 4,

    /// <summary>
    /// The thing the request names does not exist. Server-side configuration or wiring lookups are
    /// <see cref="Internal"/>, not this. HTTP 404, gRPC <c>NOT_FOUND</c>.
    /// </summary>
    NotFound = 5,

    /// <summary>
    /// The request conflicts with the current state (a version mismatch, an operation in progress, an invalid
    /// transition). HTTP 409, gRPC <c>ABORTED</c>.
    /// </summary>
    Conflict = 6,

    /// <summary>
    /// The request creates something that already exists (a duplicate). HTTP 409, gRPC <c>ALREADY_EXISTS</c>.
    /// </summary>
    AlreadyExists = 7,

    /// <summary>
    /// The system is not in the state the operation requires. Its HTTP status is provisional (400) and owned by
    /// #3420; gRPC <c>FAILED_PRECONDITION</c>.
    /// </summary>
    FailedPrecondition = 8,

    /// <summary>
    /// The caller exceeded a rate. HTTP 429 with <c>Retry-After</c> when known, gRPC <c>RESOURCE_EXHAUSTED</c>.
    /// </summary>
    RateLimited = 9,

    /// <summary>
    /// A dependency or the service is temporarily unavailable (unreachable, throttled, open circuit). HTTP 503 with
    /// <c>Retry-After</c> when known, gRPC <c>UNAVAILABLE</c>.
    /// </summary>
    Unavailable = 10,

    /// <summary>
    /// A time budget was exceeded. HTTP 504, gRPC <c>DEADLINE_EXCEEDED</c>.
    /// </summary>
    Timeout = 11,

    /// <summary>
    /// A dependency answered with an error or an unreadable response; its status is never passed through.
    /// HTTP 502, gRPC <c>UNAVAILABLE</c>.
    /// </summary>
    UpstreamFailure = 12,

    /// <summary>
    /// The caller cancelled the operation; never counted as a server fault. HTTP 499, gRPC <c>CANCELLED</c>.
    /// </summary>
    Cancelled = 13,
}
