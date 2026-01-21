using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace Encina.Modules.Isolation;

/// <summary>
/// Exception thrown when a module attempts to access schemas it is not authorized to access.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown by database interceptors and validators when they detect
/// a SQL statement that references schemas outside the module's allowed list.
/// </para>
/// <para>
/// The exception provides detailed information about:
/// <list type="bullet">
/// <item><description>The module that attempted the access</description></item>
/// <item><description>The schemas the module tried to access</description></item>
/// <item><description>The schemas the module is allowed to access</description></item>
/// </list>
/// </para>
/// <para>
/// This exception should typically be handled by letting it propagate to cause
/// the request to fail. This ensures that module isolation violations are caught
/// during development and testing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// try
/// {
///     await dbContext.SaveChangesAsync();
/// }
/// catch (ModuleIsolationViolationException ex)
/// {
///     _logger.LogError(
///         "Module '{Module}' tried to access unauthorized schemas: {Schemas}",
///         ex.ModuleName,
///         string.Join(", ", ex.AttemptedSchemas));
///     throw;
/// }
/// </code>
/// </example>
public sealed class ModuleIsolationViolationException : Exception
{
    /// <summary>
    /// The error code for module isolation violations.
    /// </summary>
    public const string ErrorCode = "Encina.ModuleIsolationViolation";

    /// <summary>
    /// Gets the name of the module that attempted the unauthorized access.
    /// </summary>
    public string ModuleName { get; }

    /// <summary>
    /// Gets the schemas that the module attempted to access.
    /// </summary>
    /// <remarks>
    /// This includes all schemas referenced in the SQL statement that triggered
    /// the violation, including both authorized and unauthorized schemas.
    /// </remarks>
    public IReadOnlySet<string> AttemptedSchemas { get; }

    /// <summary>
    /// Gets the schemas that the module is allowed to access.
    /// </summary>
    /// <remarks>
    /// This includes the module's own schema, shared schemas, and any additional
    /// schemas explicitly granted to the module.
    /// </remarks>
    public IReadOnlySet<string> AllowedSchemas { get; }

    /// <summary>
    /// Gets the schemas that were accessed without authorization.
    /// </summary>
    /// <remarks>
    /// This is the difference between <see cref="AttemptedSchemas"/> and
    /// <see cref="AllowedSchemas"/> - the schemas that caused the violation.
    /// </remarks>
    public IReadOnlySet<string> UnauthorizedSchemas { get; }

    /// <summary>
    /// Gets the SQL statement that caused the violation, if available.
    /// </summary>
    /// <remarks>
    /// May be <c>null</c> if the SQL could not be captured or was too large to store.
    /// In production, this may be truncated or omitted for security reasons.
    /// </remarks>
    public string? SqlStatement { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleIsolationViolationException"/> class.
    /// </summary>
    /// <param name="moduleName">The name of the module that attempted unauthorized access.</param>
    /// <param name="attemptedSchemas">The schemas the module tried to access.</param>
    /// <param name="allowedSchemas">The schemas the module is allowed to access.</param>
    public ModuleIsolationViolationException(
        string moduleName,
        IEnumerable<string> attemptedSchemas,
        IEnumerable<string> allowedSchemas)
        : this(moduleName, attemptedSchemas, allowedSchemas, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleIsolationViolationException"/> class.
    /// </summary>
    /// <param name="moduleName">The name of the module that attempted unauthorized access.</param>
    /// <param name="attemptedSchemas">The schemas the module tried to access.</param>
    /// <param name="allowedSchemas">The schemas the module is allowed to access.</param>
    /// <param name="sqlStatement">The SQL statement that caused the violation.</param>
    public ModuleIsolationViolationException(
        string moduleName,
        IEnumerable<string> attemptedSchemas,
        IEnumerable<string> allowedSchemas,
        string? sqlStatement)
        : base(BuildMessage(moduleName, attemptedSchemas, allowedSchemas))
    {
        ModuleName = moduleName ?? throw new ArgumentNullException(nameof(moduleName));
        AttemptedSchemas = attemptedSchemas?.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase)
            ?? throw new ArgumentNullException(nameof(attemptedSchemas));
        AllowedSchemas = allowedSchemas?.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase)
            ?? throw new ArgumentNullException(nameof(allowedSchemas));
        UnauthorizedSchemas = AttemptedSchemas
            .Except(AllowedSchemas, StringComparer.OrdinalIgnoreCase)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        SqlStatement = sqlStatement;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleIsolationViolationException"/> class
    /// from a validation result.
    /// </summary>
    /// <param name="moduleName">The name of the module that attempted unauthorized access.</param>
    /// <param name="validationResult">The validation result containing schema information.</param>
    public ModuleIsolationViolationException(
        string moduleName,
        SchemaAccessValidationResult validationResult)
        : this(moduleName, validationResult, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleIsolationViolationException"/> class
    /// from a validation result.
    /// </summary>
    /// <param name="moduleName">The name of the module that attempted unauthorized access.</param>
    /// <param name="validationResult">The validation result containing schema information.</param>
    /// <param name="sqlStatement">The SQL statement that caused the violation.</param>
    public ModuleIsolationViolationException(
        string moduleName,
        SchemaAccessValidationResult validationResult,
        string? sqlStatement)
        : this(
            moduleName,
            validationResult.AccessedSchemas,
            validationResult.AllowedSchemas,
            sqlStatement)
    {
    }

    /// <summary>
    /// Creates an <see cref="EncinaError"/> from this exception.
    /// </summary>
    /// <returns>An <see cref="EncinaError"/> with the violation details.</returns>
    /// <remarks>
    /// This method allows the exception to be converted to Encina's error handling system
    /// for use with Railway Oriented Programming (Either&lt;EncinaError, T&gt;).
    /// </remarks>
    public EncinaError ToEncinaError()
    {
        var details = new Dictionary<string, object?>
        {
            ["moduleName"] = ModuleName,
            ["attemptedSchemas"] = AttemptedSchemas.ToList(),
            ["allowedSchemas"] = AllowedSchemas.ToList(),
            ["unauthorizedSchemas"] = UnauthorizedSchemas.ToList()
        };

        if (SqlStatement is not null)
        {
            details["sqlStatement"] = SqlStatement;
        }

        return EncinaErrors.Create(ErrorCode, Message, this, details);
    }

    /// <summary>
    /// Builds a descriptive error message for the violation.
    /// </summary>
    private static string BuildMessage(
        string moduleName,
        IEnumerable<string> attemptedSchemas,
        IEnumerable<string> allowedSchemas)
    {
        var attempted = attemptedSchemas.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allowed = allowedSchemas.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unauthorized = attempted.Except(allowed, StringComparer.OrdinalIgnoreCase).ToList();

        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"Module '{moduleName}' attempted to access unauthorized schemas. ");
        sb.Append(CultureInfo.InvariantCulture, $"Unauthorized: [{string.Join(", ", unauthorized)}]. ");
        sb.Append(CultureInfo.InvariantCulture, $"Allowed: [{string.Join(", ", allowed)}].");

        return sb.ToString();
    }
}
