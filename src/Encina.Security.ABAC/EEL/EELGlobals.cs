using System.Diagnostics.CodeAnalysis;

namespace Encina.Security.ABAC.EEL;

/// <summary>
/// Global variables available to EEL (Encina Expression Language) scripts during evaluation.
/// </summary>
/// <remarks>
/// <para>
/// EEL expressions are compiled as C# scripts via Roslyn, and this class serves as the
/// globals object that provides access to the four XACML 3.0 attribute categories as
/// <c>dynamic</c> objects (typically <see cref="System.Dynamic.ExpandoObject"/> instances).
/// </para>
/// <para>
/// Property names use lowercase to provide a natural syntax in EEL expressions:
/// <c>user.department == "Finance" &amp;&amp; resource.amount > 10000</c>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var globals = new EELGlobals
/// {
///     user = CreateExpando(new { department = "Finance", role = "Manager" }),
///     resource = CreateExpando(new { amount = 50000, classification = "Confidential" }),
///     environment = CreateExpando(new { currentTime = DateTime.UtcNow }),
///     action = CreateExpando(new { name = "read" })
/// };
/// </code>
/// </example>
[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Lowercase names for EEL script readability.")]
[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Lowercase names for EEL script readability.")]
[SuppressMessage("Naming", "CA1720:Identifiers should not contain type names", Justification = "Property names match XACML attribute categories.")]
public sealed class EELGlobals
{
    /// <summary>
    /// Subject attributes — describes the entity requesting access (the user or service).
    /// </summary>
    /// <remarks>
    /// Typically an <see cref="System.Dynamic.ExpandoObject"/> populated with subject attributes
    /// such as <c>department</c>, <c>role</c>, <c>clearanceLevel</c>, etc.
    /// </remarks>
    [SuppressMessage("Naming", "CA1002:Do not expose generic lists", Justification = "Dynamic type for EEL script flexibility.")]
    public dynamic user { get; set; } = null!;

    /// <summary>
    /// Resource attributes — describes the resource being accessed.
    /// </summary>
    /// <remarks>
    /// Typically an <see cref="System.Dynamic.ExpandoObject"/> populated with resource attributes
    /// such as <c>resourceType</c>, <c>classification</c>, <c>owner</c>, etc.
    /// </remarks>
    [SuppressMessage("Naming", "CA1002:Do not expose generic lists", Justification = "Dynamic type for EEL script flexibility.")]
    public dynamic resource { get; set; } = null!;

    /// <summary>
    /// Environment attributes — describes the current environmental conditions.
    /// </summary>
    /// <remarks>
    /// Typically an <see cref="System.Dynamic.ExpandoObject"/> populated with environment attributes
    /// such as <c>currentTime</c>, <c>ipAddress</c>, <c>businessHours</c>, etc.
    /// </remarks>
    [SuppressMessage("Naming", "CA1002:Do not expose generic lists", Justification = "Dynamic type for EEL script flexibility.")]
    public dynamic environment { get; set; } = null!;

    /// <summary>
    /// Action attributes — describes the action being performed on the resource.
    /// </summary>
    /// <remarks>
    /// Typically an <see cref="System.Dynamic.ExpandoObject"/> populated with action attributes
    /// such as <c>name</c> (read, write, delete), <c>httpMethod</c>, etc.
    /// </remarks>
    [SuppressMessage("Naming", "CA1002:Do not expose generic lists", Justification = "Dynamic type for EEL script flexibility.")]
    public dynamic action { get; set; } = null!;
}
