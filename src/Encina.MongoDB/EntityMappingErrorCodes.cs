namespace Encina.MongoDB;

/// <summary>
/// Error codes for entity mapping builder operations.
/// </summary>
public static class EntityMappingErrorCodes
{
    /// <summary>
    /// The collection name was not configured in the entity mapping.
    /// </summary>
    public const string MissingTableName = "mongodb.mapping.missing_collection_name";

    /// <summary>
    /// The primary key was not configured in the entity mapping.
    /// </summary>
    public const string MissingPrimaryKey = "mongodb.mapping.missing_primary_key";

    /// <summary>
    /// No property mappings were configured in the entity mapping.
    /// </summary>
    public const string MissingColumnMappings = "mongodb.mapping.missing_property_mappings";

    /// <summary>
    /// The tenant field was not configured in the tenant entity mapping.
    /// </summary>
    public const string MissingTenantColumn = "mongodb.mapping.missing_tenant_field";
}
