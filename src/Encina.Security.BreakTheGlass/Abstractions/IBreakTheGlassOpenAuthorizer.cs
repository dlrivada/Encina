using LanguageExt;

namespace Encina.Security.BreakTheGlass;

/// <summary>
/// Decides who may open and who may review a break-the-glass elevation.
/// </summary>
/// <remarks>
/// The default, <see cref="RoleBreakTheGlassAuthorizer"/>, is fail-closed. Replace it to back the
/// decision with ABAC or another policy engine.
/// </remarks>
public interface IBreakTheGlassOpenAuthorizer
{
    /// <summary>Decides whether the identity may open an elevation for the scope.</summary>
    /// <param name="identity">The caller.</param>
    /// <param name="scope">The scope.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Unit when allowed; a refusal otherwise.</returns>
    ValueTask<Either<EncinaError, Unit>> AuthorizeOpenAsync(RequestIdentity identity, string scope, CancellationToken cancellationToken = default);

    /// <summary>Decides whether the identity may review the elevation.</summary>
    /// <param name="identity">The caller.</param>
    /// <param name="elevation">The elevation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Unit when allowed; a refusal otherwise.</returns>
    ValueTask<Either<EncinaError, Unit>> AuthorizeReviewAsync(RequestIdentity identity, BreakTheGlassElevation elevation, CancellationToken cancellationToken = default);
}
