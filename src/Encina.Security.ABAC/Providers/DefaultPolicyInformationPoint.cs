namespace Encina.Security.ABAC.Providers;

/// <summary>
/// Default implementation of <see cref="IPolicyInformationPoint"/> that returns
/// <see cref="AttributeBag.Empty"/> for all attribute resolution requests.
/// </summary>
/// <remarks>
/// <para>
/// This minimal PIP serves as a placeholder when no external attribute sources
/// are configured. It always returns an empty bag, which means:
/// </para>
/// <list type="bullet">
/// <item><description>
/// Attributes with <see cref="AttributeDesignator.MustBePresent"/> = <c>true</c>
/// will cause <see cref="Effect.Indeterminate"/> during evaluation.
/// </description></item>
/// <item><description>
/// Attributes with <see cref="AttributeDesignator.MustBePresent"/> = <c>false</c>
/// will produce an empty bag and evaluation continues.
/// </description></item>
/// </list>
/// <para>
/// Replace this with a custom implementation to resolve attributes from databases,
/// LDAP, HTTP context, or other external sources.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var pip = new DefaultPolicyInformationPoint();
/// var bag = await pip.ResolveAttributeAsync(designator);
/// // bag is always AttributeBag.Empty
/// </code>
/// </example>
public sealed class DefaultPolicyInformationPoint : IPolicyInformationPoint
{
    /// <inheritdoc />
    /// <remarks>
    /// Always returns <see cref="AttributeBag.Empty"/>. Override by registering
    /// a custom <see cref="IPolicyInformationPoint"/> implementation.
    /// </remarks>
    public ValueTask<AttributeBag> ResolveAttributeAsync(
        AttributeDesignator designator,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(AttributeBag.Empty);
    }
}
