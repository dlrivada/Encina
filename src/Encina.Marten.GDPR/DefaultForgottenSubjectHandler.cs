using Encina.Marten.GDPR.Abstractions;

using Microsoft.Extensions.Logging;

namespace Encina.Marten.GDPR;

/// <summary>
/// Default implementation of <see cref="IForgottenSubjectHandler"/> that logs when a
/// forgotten subject's encrypted field is encountered during deserialization.
/// </summary>
/// <remarks>
/// <para>
/// This handler is a no-op beyond structured logging. When the serializer encounters an
/// encrypted PII field for a forgotten subject (whose keys have been deleted), it invokes
/// this handler to notify the application. The serializer itself handles the fallback
/// behavior (e.g., returning <c>null</c> or a placeholder value).
/// </para>
/// <para>
/// Applications can register a custom <see cref="IForgottenSubjectHandler"/> implementation
/// to perform additional actions such as:
/// <list type="bullet">
/// <item><description>Updating projection caches with placeholder values</description></item>
/// <item><description>Emitting metrics for compliance monitoring</description></item>
/// <item><description>Triggering downstream cleanup workflows</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class DefaultForgottenSubjectHandler : IForgottenSubjectHandler
{
    private readonly ILogger<DefaultForgottenSubjectHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultForgottenSubjectHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured diagnostic logging.</param>
    public DefaultForgottenSubjectHandler(ILogger<DefaultForgottenSubjectHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueTask HandleForgottenSubjectAsync(
        string subjectId,
        string propertyName,
        Type eventType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Encountered forgotten subject {SubjectId} while deserializing field '{PropertyName}' on event type {EventType}. " +
            "The field value will be returned as null",
            subjectId,
            propertyName,
            eventType.Name);

        return ValueTask.CompletedTask;
    }
}
