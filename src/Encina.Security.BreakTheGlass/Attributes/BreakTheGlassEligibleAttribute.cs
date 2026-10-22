namespace Encina.Security.BreakTheGlass;

/// <summary>
/// Marks a request type as one that an active break-the-glass elevation may unlock when the normal
/// authorization gates deny it.
/// </summary>
/// <remarks>
/// <para>
/// A request type that is not marked can never be opened by an elevation, so administrative, bulk
/// and destructive request types stay out of reach. The data subject is read from the property named
/// by <see cref="SubjectIdProperty"/>, never from the caller.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [BreakTheGlassEligible("clinical-record", SubjectIdProperty = nameof(PatientId))]
/// public sealed record GetClinicalRecordQuery(string PatientId) : IQuery&lt;ClinicalRecord&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class BreakTheGlassEligibleAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BreakTheGlassEligibleAttribute"/> class.
    /// </summary>
    /// <param name="scope">The named scope an elevation must carry to unlock this request type (case-sensitive).</param>
    /// <exception cref="ArgumentException"><paramref name="scope"/> is null, empty or whitespace.</exception>
    public BreakTheGlassEligibleAttribute(string scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        Scope = scope;
    }

    /// <summary>Gets the named scope.</summary>
    public string Scope { get; }

    /// <summary>
    /// Gets or sets the name of the request property that holds the data subject id.
    /// </summary>
    /// <remarks>Required: a missing or unconvertible property fails at first use, it never silently matches.</remarks>
    public string? SubjectIdProperty { get; set; }
}
