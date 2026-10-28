using Encina.Security.Audit;
using LanguageExt;

namespace Encina.Security.BreakTheGlass;

/// <summary>
/// Opens, revokes and reviews break-the-glass elevations for the caller of the current request.
/// </summary>
/// <remarks>
/// Every method reads the caller and the tenant from the ambient request context and returns
/// <c>Either</c>; business outcomes never throw.
/// </remarks>
public interface IBreakTheGlassService
{
    /// <summary>Opens an elevation.</summary>
    /// <remarks>
    /// The justification should be operational (for example "on call, unassigned patient, crisis"), not
    /// clinical detail: data minimisation, GDPR Art. 5(1)(c) and Art. 9.
    /// </remarks>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The opened elevation.</returns>
    ValueTask<Either<EncinaError, BreakTheGlassElevation>> OpenAsync(BreakTheGlassRequest request, CancellationToken cancellationToken);

    /// <summary>Opens an elevation.</summary>
    /// <param name="subjectId">The data subject.</param>
    /// <param name="scope">The scope.</param>
    /// <param name="justification">Why the access is needed.</param>
    /// <param name="duration">How long the elevation lasts.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The opened elevation.</returns>
    ValueTask<Either<EncinaError, BreakTheGlassElevation>> OpenAsync(string subjectId, string scope, string justification, TimeSpan duration, CancellationToken cancellationToken = default);

    /// <summary>Revokes an active elevation (the opener or a reviewer may).</summary>
    /// <param name="elevationId">The elevation.</param>
    /// <param name="reason">A short reason code (never personal data).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> RevokeAsync(Guid elevationId, string reason, CancellationToken cancellationToken = default);

    /// <summary>Records the second person's ruling on an elevation.</summary>
    /// <param name="elevationId">The elevation.</param>
    /// <param name="outcome">The ruling; Unjustified revokes an active elevation.</param>
    /// <param name="note">An optional note.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> ReviewAsync(Guid elevationId, BreakTheGlassReviewOutcome outcome, string? note, CancellationToken cancellationToken = default);

    /// <summary>Lists the elevations waiting for review (a reviewer role is required).</summary>
    /// <param name="query">Filters and paging.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>One page, oldest first.</returns>
    ValueTask<Either<EncinaError, PagedResult<BreakTheGlassElevation>>> GetReviewQueueAsync(BreakTheGlassReviewQuery query, CancellationToken cancellationToken = default);

    /// <summary>Gets one elevation (the opener or a reviewer may).</summary>
    /// <param name="elevationId">The elevation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The elevation.</returns>
    ValueTask<Either<EncinaError, BreakTheGlassElevation>> GetAsync(Guid elevationId, CancellationToken cancellationToken = default);
}
