namespace Encina.Messaging.SoftDelete;

/// <summary>
/// Default implementation of <see cref="ISoftDeleteFilterContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is a simple state holder registered with scoped lifetime. Each HTTP request
/// or message processing scope gets its own instance, ensuring thread safety
/// and isolation between concurrent requests.
/// </para>
/// <para>
/// The default value of <see cref="IncludeDeleted"/> is <c>false</c>, meaning
/// soft-deleted entities are excluded from queries by default.
/// </para>
/// </remarks>
public sealed class SoftDeleteFilterContext : ISoftDeleteFilterContext
{
    /// <inheritdoc />
    public bool IncludeDeleted { get; set; }

    /// <inheritdoc />
    public void Reset()
    {
        IncludeDeleted = false;
    }
}
