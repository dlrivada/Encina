namespace Encina.Cdc.Messaging;

/// <summary>
/// Configuration options for the CDC-to-messaging bridge.
/// Controls which change events are published as notifications and how topics are named.
/// </summary>
/// <remarks>
/// <para>
/// All filters are inclusive: an empty <see cref="IncludeTables"/> array means all tables
/// are included. The <see cref="ExcludeTables"/> filter takes precedence over <see cref="IncludeTables"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// config.WithMessagingBridge(opts =>
/// {
///     opts.TopicPattern = "cdc.{tableName}.{operation}";
///     opts.IncludeTables = ["Orders", "Customers"];
///     opts.ExcludeTables = ["__EFMigrationsHistory"];
///     opts.IncludeOperations = [ChangeOperation.Insert, ChangeOperation.Update];
/// });
/// </code>
/// </example>
public sealed class CdcMessagingOptions
{
    /// <summary>
    /// Gets or sets the topic name pattern for published notifications.
    /// Supports <c>{tableName}</c> and <c>{operation}</c> placeholders.
    /// Default is <c>{tableName}.{operation}</c>.
    /// </summary>
    public string TopicPattern { get; set; } = "{tableName}.{operation}";

    /// <summary>
    /// Gets or sets the table names to include for messaging.
    /// Empty array means all tables are included.
    /// </summary>
    public string[] IncludeTables { get; set; } = [];

    /// <summary>
    /// Gets or sets the table names to exclude from messaging.
    /// Takes precedence over <see cref="IncludeTables"/>.
    /// </summary>
    public string[] ExcludeTables { get; set; } = [];

    /// <summary>
    /// Gets or sets the change operations to include for messaging.
    /// Empty array means all operations are included.
    /// </summary>
    public ChangeOperation[] IncludeOperations { get; set; } = [];

    /// <summary>
    /// Determines whether a change event should be published based on the configured filters.
    /// </summary>
    /// <param name="tableName">The table name to check.</param>
    /// <param name="operation">The change operation to check.</param>
    /// <returns><c>true</c> if the event passes all filters; otherwise, <c>false</c>.</returns>
    internal bool ShouldPublish(string tableName, ChangeOperation operation)
    {
        // Exclude filter takes precedence
        if (ExcludeTables.Length > 0 &&
            Array.Exists(ExcludeTables, t => string.Equals(t, tableName, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        // Include tables filter
        if (IncludeTables.Length > 0 &&
            !Array.Exists(IncludeTables, t => string.Equals(t, tableName, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        // Include operations filter
        if (IncludeOperations.Length > 0 &&
            !Array.Exists(IncludeOperations, o => o == operation))
        {
            return false;
        }

        return true;
    }
}
