namespace Encina.ADO.SqlServer;

/// <summary>
/// Error codes for entity mapping builder operations.
/// </summary>
public static class EntityMappingErrorCodes
{
    /// <summary>
    /// The table name was not configured in the entity mapping.
    /// </summary>
    public const string MissingTableName = "ado.sqlserver.mapping.missing_table_name";

    /// <summary>
    /// The primary key was not configured in the entity mapping.
    /// </summary>
    public const string MissingPrimaryKey = "ado.sqlserver.mapping.missing_primary_key";

    /// <summary>
    /// No column mappings were configured in the entity mapping.
    /// </summary>
    public const string MissingColumnMappings = "ado.sqlserver.mapping.missing_column_mappings";

    /// <summary>
    /// The tenant column was not configured in the tenant entity mapping.
    /// </summary>
    public const string MissingTenantColumn = "ado.sqlserver.mapping.missing_tenant_column";
}
