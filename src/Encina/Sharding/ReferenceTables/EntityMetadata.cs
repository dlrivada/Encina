using System.Reflection;

namespace Encina.Sharding.ReferenceTables;

/// <summary>
/// Metadata about a single property on a reference table entity, used for
/// dynamic SQL generation and data reader mapping.
/// </summary>
public sealed record PropertyMetadata(
    PropertyInfo Property,
    string ColumnName,
    bool IsPrimaryKey);

/// <summary>
/// Cached metadata for a reference table entity type, discovered via reflection
/// and attribute conventions.
/// </summary>
/// <remarks>
/// <para>
/// Table name is resolved from <see cref="System.ComponentModel.DataAnnotations.Schema.TableAttribute"/>
/// if present, otherwise falls back to the type name.
/// </para>
/// <para>
/// The primary key is identified by <see cref="System.ComponentModel.DataAnnotations.KeyAttribute"/>
/// or by convention (a property named "Id", case-insensitive).
/// </para>
/// <para>
/// Column names are resolved from <see cref="System.ComponentModel.DataAnnotations.Schema.ColumnAttribute"/>
/// if present, otherwise match the property name.
/// </para>
/// </remarks>
public sealed record EntityMetadata(
    string TableName,
    PropertyMetadata PrimaryKey,
    IReadOnlyList<PropertyMetadata> AllProperties,
    IReadOnlyList<PropertyMetadata> NonKeyProperties);
