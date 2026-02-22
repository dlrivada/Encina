using LanguageExt;

namespace Encina.AspNetCore.Authorization;

/// <summary>
/// Thin facade over ASP.NET Core's <see cref="Microsoft.AspNetCore.Authorization.IAuthorizationService"/>
/// that converts authorization results into <see cref="Either{EncinaError, Boolean}"/> for
/// Railway Oriented Programming integration.
/// </summary>
/// <remarks>
/// <para>
/// This interface does <b>not</b> replace <see cref="Microsoft.AspNetCore.Authorization.IAuthorizationService"/>.
/// It wraps it, adding Encina's ROP semantics so that authorization failures are returned as
/// <see cref="EncinaError"/> values instead of requiring callers to inspect
/// <see cref="Microsoft.AspNetCore.Authorization.AuthorizationResult"/> manually.
/// </para>
/// <para>
/// Use this in handlers that need to perform resource-based authorization checks
/// after loading the resource from the database.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class UpdateOrderCommandHandler : ICommandHandler&lt;UpdateOrderCommand, Order&gt;
/// {
///     private readonly IResourceAuthorizer _authorizer;
///     private readonly IOrderRepository _orders;
///
///     public async Task&lt;Either&lt;EncinaError, Order&gt;&gt; Handle(
///         UpdateOrderCommand command, CancellationToken ct)
///     {
///         var order = await _orders.GetByIdAsync(command.OrderId, ct);
///
///         var authResult = await _authorizer.AuthorizeAsync(order, "CanEditOrder", ct);
///         if (authResult.IsLeft)
///             return authResult.Map&lt;Order&gt;(_ =&gt; default!);
///
///         // proceed with update...
///     }
/// }
/// </code>
/// </example>
public interface IResourceAuthorizer
{
    /// <summary>
    /// Evaluates the specified ASP.NET Core authorization policy against a resource.
    /// </summary>
    /// <typeparam name="TResource">The type of the resource to authorize against.</typeparam>
    /// <param name="resource">The resource instance passed to the policy handler.</param>
    /// <param name="policy">The name of the ASP.NET Core authorization policy to evaluate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Either{EncinaError, Boolean}"/>:
    /// <c>Right(true)</c> when the policy is satisfied,
    /// <c>Left(error)</c> with code <see cref="EncinaErrorCodes.AuthorizationResourceDenied"/> when denied,
    /// or <c>Left(error)</c> with code <see cref="EncinaErrorCodes.AuthorizationUnauthorized"/> when
    /// no authenticated user is available.
    /// </returns>
    Task<Either<EncinaError, bool>> AuthorizeAsync<TResource>(
        TResource resource,
        string policy,
        CancellationToken cancellationToken);

    /// <summary>
    /// Evaluates the specified ASP.NET Core authorization policy against a resource (non-generic overload).
    /// </summary>
    /// <param name="resource">The resource instance passed to the policy handler.</param>
    /// <param name="policy">The name of the ASP.NET Core authorization policy to evaluate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Either{EncinaError, Boolean}"/>:
    /// <c>Right(true)</c> when the policy is satisfied,
    /// <c>Left(error)</c> with code <see cref="EncinaErrorCodes.AuthorizationResourceDenied"/> when denied.
    /// </returns>
    Task<Either<EncinaError, bool>> AuthorizeAsync(
        object resource,
        string policy,
        CancellationToken cancellationToken);
}
