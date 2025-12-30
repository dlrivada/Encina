namespace Encina.Testing.Respawn;

/// <summary>
/// Configuration options for Respawn database cleanup.
/// </summary>
public sealed class RespawnOptions
{
    /// <summary>
    /// Gets or sets the tables to ignore during cleanup.
    /// These tables will not have their data deleted.
    /// </summary>
    /// <remarks>
    /// Common tables to ignore include migration history tables
    /// like "__EFMigrationsHistory" or "__schema_versions".
    /// </remarks>
    public string[] TablesToIgnore { get; set; } = [];

    /// <summary>
    /// Gets or sets the schemas to include in cleanup.
    /// If empty, all schemas are included.
    /// </summary>
    public string[] SchemasToInclude { get; set; } = [];

    /// <summary>
    /// Gets or sets the schemas to exclude from cleanup.
    /// </summary>
    public string[] SchemasToExclude { get; set; } = [];

    /// <summary>
    /// Gets or sets whether to reset Encina messaging tables (Outbox, Inbox, Saga, Scheduling).
    /// Default is true.
    /// </summary>
    public bool ResetEncinaMessagingTables { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to check for temporal tables (SQL Server feature).
    /// Default is false for performance.
    /// </summary>
    public bool CheckTemporalTables { get; set; }

    /// <summary>
    /// Gets or sets whether to use WITH (TABLOCKX) hint for SQL Server.
    /// Improves performance but may cause blocking in concurrent scenarios.
    /// Default is true.
    /// </summary>
    public bool WithReseed { get; set; } = true;

    /// <summary>
    /// Gets the default Encina messaging tables.
    /// </summary>
    public static readonly string[] EncinaMessagingTables =
    [
        "OutboxMessages",
        "InboxMessages",
        "SagaStates",
        "ScheduledMessages"
    ];
}
