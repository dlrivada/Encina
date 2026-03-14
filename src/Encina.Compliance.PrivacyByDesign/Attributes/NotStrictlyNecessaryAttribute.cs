namespace Encina.Compliance.PrivacyByDesign;

/// <summary>
/// Marks a property as not strictly necessary for the declared processing purpose.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a property of a request type that is decorated with
/// <see cref="EnforceDataMinimizationAttribute"/>, the pipeline behavior includes this
/// field in the "unnecessary" category of the <see cref="Model.MinimizationReport"/>.
/// </para>
/// <para>
/// Per GDPR Article 25(2), "only personal data which are necessary for each specific
/// purpose of the processing are processed." Fields marked with this attribute are
/// acknowledged as not necessary and are monitored for active use.
/// </para>
/// <para>
/// A violation is reported only when the field has a non-default value in the request,
/// indicating that unnecessary data is being actively collected.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [EnforceDataMinimization(Purpose = "User Registration")]
/// public sealed record RegisterUserCommand(
///     string Email,
///     string Password,
///     [property: NotStrictlyNecessary(Reason = "Marketing preferences, not required for registration")]
///     bool? SubscribeToNewsletter,
///     [property: NotStrictlyNecessary(
///         Reason = "Optional demographics for analytics",
///         Severity = MinimizationSeverity.Warning)]
///     string? DateOfBirth) : ICommand&lt;UserId&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class NotStrictlyNecessaryAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the reason why this field is not strictly necessary.
    /// </summary>
    /// <remarks>
    /// Provides context for compliance officers and developers to understand
    /// why this data is collected despite not being required for the processing purpose.
    /// </remarks>
    public required string Reason { get; set; }

    /// <summary>
    /// Gets or sets the severity level of the minimization finding when this field has a value.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="Model.MinimizationSeverity.Warning"/>. Use
    /// <see cref="Model.MinimizationSeverity.Violation"/> for fields that should never
    /// be populated in production, or <see cref="Model.MinimizationSeverity.Info"/> for
    /// fields that are tracked but not actionable.
    /// </remarks>
    public Model.MinimizationSeverity Severity { get; set; } = Model.MinimizationSeverity.Warning;
}
