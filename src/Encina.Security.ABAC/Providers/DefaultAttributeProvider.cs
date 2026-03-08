namespace Encina.Security.ABAC.Providers;

/// <summary>
/// Default implementation of <see cref="IAttributeProvider"/> that returns empty
/// dictionaries for all attribute categories.
/// </summary>
/// <remarks>
/// <para>
/// This minimal provider serves as a placeholder when no application-specific
/// attribute sources are configured. It returns empty dictionaries for subject,
/// resource, and environment attributes.
/// </para>
/// <para>
/// Applications should replace this with a custom implementation that extracts
/// attributes from claims, databases, HTTP context, or other domain-specific sources.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var provider = new DefaultAttributeProvider();
/// var subjectAttrs = await provider.GetSubjectAttributesAsync("user-123");
/// // subjectAttrs is an empty dictionary
/// </code>
/// </example>
public sealed class DefaultAttributeProvider : IAttributeProvider
{
    private static readonly IReadOnlyDictionary<string, object> EmptyAttributes =
        new Dictionary<string, object>().AsReadOnly();

    /// <inheritdoc />
    /// <remarks>
    /// Always returns an empty dictionary. Override by registering a custom
    /// <see cref="IAttributeProvider"/> implementation that resolves subject
    /// attributes from claims, user databases, or identity providers.
    /// </remarks>
    public ValueTask<IReadOnlyDictionary<string, object>> GetSubjectAttributesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(EmptyAttributes);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Always returns an empty dictionary. Override by registering a custom
    /// <see cref="IAttributeProvider"/> implementation that resolves resource
    /// attributes from the domain model or resource metadata.
    /// </remarks>
    public ValueTask<IReadOnlyDictionary<string, object>> GetResourceAttributesAsync<TResource>(
        TResource resource,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(EmptyAttributes);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Always returns an empty dictionary. Override by registering a custom
    /// <see cref="IAttributeProvider"/> implementation that resolves environment
    /// attributes such as current time, IP address, or security clearance levels.
    /// </remarks>
    public ValueTask<IReadOnlyDictionary<string, object>> GetEnvironmentAttributesAsync(
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(EmptyAttributes);
    }
}
