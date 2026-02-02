namespace Encina.Marten;

/// <summary>
/// Interface for enriching event metadata before persistence.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to add custom metadata headers to events at persistence time.
/// Enrichers are invoked by the aggregate repository before events are appended to the store.
/// </para>
/// <para>
/// Common use cases include:
/// <list type="bullet">
/// <item>Adding build information (commit SHA, version)</item>
/// <item>Adding environment metadata (deployment region, pod name)</item>
/// <item>Adding request-specific data not available in IRequestContext</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class BuildInfoEnricher : IEventMetadataEnricher
/// {
///     private readonly string _commitSha;
///     private readonly string _version;
///
///     public BuildInfoEnricher(IOptions&lt;BuildInfo&gt; buildInfo)
///     {
///         _commitSha = buildInfo.Value.CommitSha;
///         _version = buildInfo.Value.Version;
///     }
///
///     public IDictionary&lt;string, object&gt; EnrichMetadata(
///         object domainEvent,
///         IRequestContext context)
///     {
///         return new Dictionary&lt;string, object&gt;
///         {
///             ["CommitSha"] = _commitSha,
///             ["Version"] = _version
///         };
///     }
/// }
/// </code>
/// </example>
public interface IEventMetadataEnricher
{
    /// <summary>
    /// Enriches event metadata with additional headers.
    /// </summary>
    /// <param name="domainEvent">The domain event being persisted.</param>
    /// <param name="context">The current request context.</param>
    /// <returns>
    /// A dictionary of additional headers to include with the event.
    /// Return an empty dictionary if no additional headers should be added.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is called once per event during persistence operations.
    /// Headers returned by all registered enrichers are merged before storing.
    /// </para>
    /// <para>
    /// The method should be fast and should not perform I/O operations.
    /// If enricher fails, the event persistence continues without the enricher's headers.
    /// </para>
    /// </remarks>
    IDictionary<string, object> EnrichMetadata(object domainEvent, IRequestContext context);
}
