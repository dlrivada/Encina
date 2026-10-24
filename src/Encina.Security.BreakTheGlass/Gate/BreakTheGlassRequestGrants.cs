using System.Collections.Concurrent;

namespace Encina.Security.BreakTheGlass;

/// <summary>
/// Scoped holder of the grants given during one request, so a second gate that denies the same request
/// reuses the grant and the access is audited once.
/// </summary>
/// <remarks>
/// A grant is keyed by correlation id, request type and subject id: it is never reused for another
/// request type or another data subject of the same correlation id.
/// </remarks>
internal sealed class BreakTheGlassRequestGrants
{
    private readonly ConcurrentDictionary<(string CorrelationId, Type RequestType, string SubjectId), AccessElevation> _grants = new();

    public bool TryGet(string correlationId, Type requestType, string subjectId, out AccessElevation grant) =>
        _grants.TryGetValue((correlationId, requestType, subjectId), out grant!);

    public void Set(string correlationId, Type requestType, string subjectId, AccessElevation grant) =>
        _grants[(correlationId, requestType, subjectId)] = grant;
}
