namespace Encina.Testing.Testcontainers;

/// <summary>
/// Holds the default container image of each provider, defined once and read by every
/// <see cref="EncinaContainers"/> overload and every pre-configured fixture.
/// </summary>
/// <remarks>
/// Each default is the vendor's supported LTS line (or the stable line where the vendor has no LTS).
/// Callers that need another image pass it with <c>WithImage(...)</c>.
/// </remarks>
internal static class ContainerDefaultImages
{
    /// <summary>
    /// The MySQL LTS line.
    /// </summary>
    internal const string MySql = "mysql:8.4";
}
