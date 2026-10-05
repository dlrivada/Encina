using Encina.Marten.GDPR.Abstractions;
using Encina.Marten.GDPR.Diagnostics;

using Microsoft.Extensions.Logging;

namespace Encina.Marten.GDPR;

/// <summary>
/// Default implementation of <see cref="IForgottenSubjectHandler"/> that logs when data of a forgotten subject
/// is read.
/// </summary>
/// <remarks>
/// <para>
/// This handler is a no-op beyond structured logging (event 8477, with the root document type and the field path,
/// never the subject id). The serializer itself applies the anonymized placeholder to the fields of the forgotten
/// subject before it calls the handler.
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
        string fieldPath,
        Type documentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fieldPath);
        ArgumentNullException.ThrowIfNull(documentType);

        // The data subject's own identifier is never logged (#1429, following #1314).
        _logger.ForgottenSubjectEncountered(documentType.Name, fieldPath);
        return ValueTask.CompletedTask;
    }
}
