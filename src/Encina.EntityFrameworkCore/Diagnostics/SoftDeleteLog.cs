using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.EntityFrameworkCore.Diagnostics;

/// <summary>
/// High-performance logging methods for soft delete operations using LoggerMessage source generators.
/// </summary>
/// <remarks>
/// <para>
/// Event IDs are allocated in the 1500-1599 range to avoid collisions with other Encina modules.
/// </para>
/// <para>
/// This class uses source generators to create optimized logging methods.
/// Excluded from code coverage as the generated code is boilerplate.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public static partial class SoftDeleteLog
{
    /// <summary>
    /// Logs that a soft delete operation is starting for the specified entity type.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type name of the entity being soft-deleted.</param>
    [LoggerMessage(
        EventId = 1500,
        Level = LogLevel.Debug,
        Message = "Soft-deleting entity {EntityType}")]
    public static partial void SoftDeleting(
        ILogger logger,
        string entityType);

    /// <summary>
    /// Logs that an entity was successfully soft-deleted.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type name of the entity that was soft-deleted.</param>
    [LoggerMessage(
        EventId = 1501,
        Level = LogLevel.Information,
        Message = "Entity {EntityType} soft-deleted")]
    public static partial void SoftDeleted(
        ILogger logger,
        string entityType);

    /// <summary>
    /// Logs that a restore operation is starting for a soft-deleted entity.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type name of the entity being restored.</param>
    [LoggerMessage(
        EventId = 1502,
        Level = LogLevel.Debug,
        Message = "Restoring soft-deleted entity {EntityType}")]
    public static partial void Restoring(
        ILogger logger,
        string entityType);

    /// <summary>
    /// Logs that a soft-deleted entity was successfully restored.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type name of the entity that was restored.</param>
    [LoggerMessage(
        EventId = 1503,
        Level = LogLevel.Information,
        Message = "Entity {EntityType} restored")]
    public static partial void Restored(
        ILogger logger,
        string entityType);

    /// <summary>
    /// Logs that a hard delete operation is starting for the specified entity type.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type name of the entity being hard-deleted.</param>
    [LoggerMessage(
        EventId = 1504,
        Level = LogLevel.Debug,
        Message = "Hard-deleting entity {EntityType}")]
    public static partial void HardDeleting(
        ILogger logger,
        string entityType);

    /// <summary>
    /// Logs that an entity was successfully hard-deleted.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type name of the entity that was hard-deleted.</param>
    [LoggerMessage(
        EventId = 1505,
        Level = LogLevel.Information,
        Message = "Entity {EntityType} hard-deleted")]
    public static partial void HardDeleted(
        ILogger logger,
        string entityType);

    /// <summary>
    /// Logs that a soft delete operation failed with an exception.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="entityType">The type name of the entity involved in the failed operation.</param>
    [LoggerMessage(
        EventId = 1506,
        Level = LogLevel.Error,
        Message = "Soft delete operation for {EntityType} failed")]
    public static partial void SoftDeleteFailed(
        ILogger logger,
        Exception exception,
        string entityType);
}
