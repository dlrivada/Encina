using System.Globalization;
using System.Text;

namespace Encina.Sharding.Colocation;

/// <summary>
/// Exception thrown when a co-location constraint is violated during startup validation.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown by the sharding service registration when it detects that
/// co-located entities do not satisfy the required constraints:
/// <list type="bullet">
/// <item>Both root and co-located entities must be shardable</item>
/// <item>Shard key types must be compatible between root and co-located entities</item>
/// <item>An entity cannot belong to more than one co-location group</item>
/// <item>An entity cannot be co-located with itself</item>
/// </list>
/// </para>
/// <para>
/// The exception provides detailed context about the validation failure, including the
/// entities involved, expected vs. actual shard key types, and remediation guidance.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// try
/// {
///     services.AddEncinaSharding&lt;Order&gt;(options =>
///     {
///         options.UseHashRouting()
///             .AddShard("shard-0", "Server=shard0;...")
///             .AddColocatedEntity&lt;OrderItem&gt;();
///     });
/// }
/// catch (ColocationViolationException ex)
/// {
///     logger.LogError(
///         "Co-location validation failed: {Reason}. Root: {Root}, Failed: {Failed}",
///         ex.Reason,
///         ex.RootEntityType?.Name,
///         ex.FailedEntityType?.Name);
/// }
/// </code>
/// </example>
public sealed class ColocationViolationException : Exception
{
    /// <summary>
    /// The error code for co-location violation exceptions.
    /// </summary>
    public const string ErrorCode = "Encina.ColocationViolation";

    /// <summary>
    /// Gets the root entity type of the co-location group where the violation occurred.
    /// </summary>
    public Type? RootEntityType { get; }

    /// <summary>
    /// Gets the entity type that failed co-location validation.
    /// </summary>
    public Type? FailedEntityType { get; }

    /// <summary>
    /// Gets the expected shard key type (from the root entity), if applicable.
    /// </summary>
    public string? ExpectedShardKeyType { get; }

    /// <summary>
    /// Gets the actual shard key type found on the failed entity, if applicable.
    /// </summary>
    public string? ActualShardKeyType { get; }

    /// <summary>
    /// Gets a human-readable description of the validation failure reason.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ColocationViolationException"/> class.
    /// </summary>
    /// <param name="rootEntityType">The root entity type of the co-location group.</param>
    /// <param name="failedEntityType">The entity type that failed validation.</param>
    /// <param name="reason">A description of the validation failure.</param>
    /// <param name="expectedShardKeyType">The expected shard key type from the root entity.</param>
    /// <param name="actualShardKeyType">The actual shard key type on the failed entity.</param>
    public ColocationViolationException(
        Type? rootEntityType,
        Type? failedEntityType,
        string reason,
        string? expectedShardKeyType = null,
        string? actualShardKeyType = null)
        : base(BuildMessage(rootEntityType, failedEntityType, reason, expectedShardKeyType, actualShardKeyType))
    {
        RootEntityType = rootEntityType;
        FailedEntityType = failedEntityType;
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        ExpectedShardKeyType = expectedShardKeyType;
        ActualShardKeyType = actualShardKeyType;
    }

    /// <summary>
    /// Creates an <see cref="EncinaError"/> from this exception.
    /// </summary>
    /// <returns>An <see cref="EncinaError"/> with the violation details.</returns>
    public EncinaError ToEncinaError()
    {
        var details = new Dictionary<string, object?>
        {
            ["rootEntityType"] = RootEntityType?.FullName,
            ["failedEntityType"] = FailedEntityType?.FullName,
            ["reason"] = Reason
        };

        if (ExpectedShardKeyType is not null)
        {
            details["expectedShardKeyType"] = ExpectedShardKeyType;
        }

        if (ActualShardKeyType is not null)
        {
            details["actualShardKeyType"] = ActualShardKeyType;
        }

        return EncinaErrors.Create(ErrorCode, Message, this, details);
    }

    private static string BuildMessage(
        Type? rootEntityType,
        Type? failedEntityType,
        string reason,
        string? expectedShardKeyType,
        string? actualShardKeyType)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"Co-location validation failed: {reason}");

        if (rootEntityType is not null)
        {
            sb.Append(CultureInfo.InvariantCulture, $" Root entity: '{rootEntityType.Name}'.");
        }

        if (failedEntityType is not null)
        {
            sb.Append(CultureInfo.InvariantCulture, $" Failed entity: '{failedEntityType.Name}'.");
        }

        if (expectedShardKeyType is not null && actualShardKeyType is not null)
        {
            sb.Append(CultureInfo.InvariantCulture,
                $" Expected shard key type: '{expectedShardKeyType}', actual: '{actualShardKeyType}'.");
        }

        sb.Append(" Ensure all co-located entities implement IShardable, ICompoundShardable, " +
                  "or have properties marked with [ShardKey], and that shard key types are compatible.");

        return sb.ToString();
    }
}
