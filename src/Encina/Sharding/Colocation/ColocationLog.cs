using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Sharding.Colocation;

/// <summary>
/// High-performance logging for co-location group operations.
/// </summary>
/// <remarks>
/// This class uses source generators to create optimized logging methods.
/// Excluded from code coverage as the generated code is boilerplate.
/// </remarks>
[ExcludeFromCodeCoverage]
internal static partial class ColocationLog
{
    [LoggerMessage(
        EventId = 620,
        Level = LogLevel.Information,
        Message = "Co-location group registered: root entity '{RootEntityName}' with {ColocatedCount} co-located entities (shared key property: '{SharedKeyProperty}')")]
    public static partial void ColocationGroupRegistered(
        ILogger logger,
        string rootEntityName,
        int colocatedCount,
        string sharedKeyProperty);

    [LoggerMessage(
        EventId = 621,
        Level = LogLevel.Error,
        Message = "Co-location validation failed for entity '{FailedEntityName}' in group rooted at '{RootEntityName}': {Reason}")]
    public static partial void ColocationValidationFailed(
        ILogger logger,
        string failedEntityName,
        string rootEntityName,
        string reason);

    [LoggerMessage(
        EventId = 622,
        Level = LogLevel.Debug,
        Message = "Co-location group routed: entity '{EntityName}' resolved to group '{RootEntityName}' on shard '{ShardId}'")]
    public static partial void ColocationGroupRouted(
        ILogger logger,
        string entityName,
        string rootEntityName,
        string shardId);

    [LoggerMessage(
        EventId = 623,
        Level = LogLevel.Warning,
        Message = "Co-location group lookup failed: entity '{EntityName}' does not belong to any co-location group")]
    public static partial void ColocationGroupNotFound(
        ILogger logger,
        string entityName);

    [LoggerMessage(
        EventId = 624,
        Level = LogLevel.Debug,
        Message = "Co-location registry initialized with {GroupCount} groups covering {TotalEntityCount} entity types")]
    public static partial void ColocationRegistryInitialized(
        ILogger logger,
        int groupCount,
        int totalEntityCount);
}
