namespace Encina.Testing.Modules;

/// <summary>
/// Specifies the type of dependency between modules.
/// </summary>
public enum DependencyType
{
    /// <summary>
    /// A direct type reference dependency.
    /// </summary>
    Direct,

    /// <summary>
    /// A dependency through a public API interface.
    /// </summary>
    PublicApi,

    /// <summary>
    /// A dependency through integration events.
    /// </summary>
    IntegrationEvent
}
