using LanguageExt;

namespace Encina.Messaging.Scheduling;

/// <summary>
/// Dispatches a deserialized scheduled message request through <see cref="IEncina"/>,
/// selecting the correct generic overload
/// (<see cref="IEncina.Send{TResponse}"/> or <see cref="IEncina.Publish{TNotification}"/>)
/// based on the runtime <see cref="Type"/> of the request.
/// </summary>
/// <remarks>
/// <para>
/// The dispatcher receives the request as an untyped <see cref="object"/> together with
/// its <see cref="Type"/>. It must bridge from this untyped representation to the
/// strongly-typed generic methods on <see cref="IEncina"/> without requiring compile-time
/// knowledge of the concrete request or response types.
/// </para>
/// <para>
/// <b>Failure signalling</b>: follows Railway Oriented Programming. Dispatch failures
/// (including unknown request shapes, errors from <see cref="IEncina"/>, and
/// deserialization issues) are returned as <c>Left(EncinaError)</c> — the dispatcher
/// MUST NOT throw exceptions to signal business failures.
/// </para>
/// <para>
/// <b>Default implementation</b>:
/// <see cref="CompiledExpressionScheduledMessageDispatcher"/> builds and caches a
/// compiled delegate per request <see cref="Type"/> via
/// <see cref="System.Linq.Expressions.Expression"/>, achieving zero reflection and
/// zero <c>dynamic</c> on the hot path after the first call per type.
/// </para>
/// <para>
/// <b>Custom implementations</b>: register your own <see cref="IScheduledMessageDispatcher"/>
/// in DI before calling <c>AddEncina*()</c>; the messaging core uses
/// <c>TryAddScoped</c> so user registrations win. A source-generator-based dispatcher
/// for full NativeAOT compatibility can be provided in a follow-up package.
/// </para>
/// </remarks>
public interface IScheduledMessageDispatcher
{
    /// <summary>
    /// Dispatches a single deserialized request through <see cref="IEncina"/>.
    /// </summary>
    /// <param name="requestType">
    /// The runtime <see cref="Type"/> of the deserialized request. This type must
    /// implement either <see cref="IRequest{TResponse}"/> (for commands/queries) or
    /// <see cref="INotification"/> (for notifications). If it implements neither, the
    /// dispatcher returns <c>Left</c> with
    /// <see cref="SchedulingErrorCodes.UnknownRequestShape"/>.
    /// </param>
    /// <param name="request">
    /// The deserialized request instance. Must be assignable to <paramref name="requestType"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token propagated to the underlying <see cref="IEncina.Send{TResponse}"/>
    /// or <see cref="IEncina.Publish{TNotification}"/> call. This allows host shutdown to
    /// promptly abort in-flight dispatch operations.
    /// </param>
    /// <returns>
    /// <c>Right(Unit.Default)</c> on successful dispatch (regardless of the response value
    /// for commands — the processor only cares about success vs failure).
    /// <c>Left(EncinaError)</c> on dispatch failure or unknown request shape.
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> DispatchAsync(
        Type requestType,
        object request,
        CancellationToken cancellationToken);
}
