namespace Encina.Messaging.ReadWriteSeparation;

/// <summary>
/// Indicates that a query class should use the primary (write) database instead of a read replica.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute to query classes that require read-after-write consistency.
/// The routing pipeline behavior detects this attribute and sets the intent to
/// <see cref="DatabaseIntent.ForceWrite"/>, ensuring the query executes against
/// the primary database.
/// </para>
/// <para>
/// <b>When to Use:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       Reading data immediately after a write in the same request or transaction
///     </description>
///   </item>
///   <item>
///     <description>
///       Queries that must see the absolute latest committed data
///     </description>
///   </item>
///   <item>
///     <description>
///       Validation queries that check uniqueness or business rules
///     </description>
///   </item>
///   <item>
///     <description>
///       Critical reads where replication lag could cause incorrect behavior
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>When NOT to Use:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       General reporting or dashboard queries
///     </description>
///   </item>
///   <item>
///     <description>
///       Queries where eventual consistency is acceptable
///     </description>
///   </item>
///   <item>
///     <description>
///       High-traffic read queries that would overload the primary database
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // This query needs to read the user immediately after creation
/// [ForceWriteDatabase]
/// public sealed record GetUserAfterCreationQuery(Guid UserId) : IQuery&lt;UserDto&gt;;
///
/// // This query checks username uniqueness during registration
/// [ForceWriteDatabase]
/// public sealed record CheckUsernameAvailableQuery(string Username) : IQuery&lt;bool&gt;;
///
/// // This reporting query can tolerate eventual consistency
/// // No attribute needed - will use read replica
/// public sealed record GetSalesReportQuery(DateRange Period) : IQuery&lt;SalesReport&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ForceWriteDatabaseAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForceWriteDatabaseAttribute"/> class.
    /// </summary>
    public ForceWriteDatabaseAttribute()
    {
    }

    /// <summary>
    /// Gets or sets a reason explaining why this query requires the primary database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property is purely for documentation purposes and has no runtime effect.
    /// It helps other developers understand why the attribute was applied.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [ForceWriteDatabase(Reason = "Must verify latest balance before withdrawal")]
    /// public sealed record GetAccountBalanceQuery(Guid AccountId) : IQuery&lt;decimal&gt;;
    /// </code>
    /// </example>
    public string? Reason { get; init; }
}
