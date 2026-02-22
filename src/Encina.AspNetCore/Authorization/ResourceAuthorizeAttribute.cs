namespace Encina.AspNetCore.Authorization;

/// <summary>
/// Marks a request type for resource-based authorization using an ASP.NET Core policy.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a request type (command or query), the
/// <see cref="AuthorizationPipelineBehavior{TRequest, TResponse}"/> evaluates
/// the specified policy against the request object acting as the <b>resource</b>.
/// This delegates to
/// <see cref="Microsoft.AspNetCore.Authorization.IAuthorizationService.AuthorizeAsync(System.Security.Claims.ClaimsPrincipal, object?, string?)"/>
/// — no parallel authorization infrastructure is created.
/// </para>
/// <para>
/// Define an <see cref="Microsoft.AspNetCore.Authorization.AuthorizationHandler{TRequirement, TResource}"/>
/// for the request type to implement the authorization logic.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // 1. Mark the command for resource-based authorization
/// [ResourceAuthorize("CanEditOrder")]
/// public record UpdateOrderCommand(OrderId Id, string NewStatus) : ICommand&lt;Order&gt;;
///
/// // 2. Register the policy
/// builder.Services.AddEncinaAuthorization(configurePolicies: policies =>
/// {
///     policies.AddPolicy("CanEditOrder", p =>
///         p.Requirements.Add(new OrderOwnerRequirement()));
/// });
///
/// // 3. Implement the handler that receives the request as resource
/// public class OrderOwnerHandler
///     : AuthorizationHandler&lt;OrderOwnerRequirement, UpdateOrderCommand&gt;
/// {
///     protected override Task HandleRequirementAsync(
///         AuthorizationHandlerContext context,
///         OrderOwnerRequirement requirement,
///         UpdateOrderCommand resource)
///     {
///         // resource.Id is available for ownership checks
///         if (IsOwner(context.User, resource.Id))
///             context.Succeed(requirement);
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class ResourceAuthorizeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceAuthorizeAttribute"/> class.
    /// </summary>
    /// <param name="policy">
    /// The name of the ASP.NET Core authorization policy to evaluate against
    /// the request object as resource.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="policy"/> is <c>null</c>, empty, or whitespace.
    /// </exception>
    public ResourceAuthorizeAttribute(string policy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policy);
        Policy = policy;
    }

    /// <summary>
    /// Gets the name of the ASP.NET Core authorization policy to evaluate.
    /// </summary>
    public string Policy { get; }
}
