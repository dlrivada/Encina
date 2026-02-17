using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Tenancy.Diagnostics;

/// <summary>
/// High-performance logging methods for multi-tenancy operations using LoggerMessage source generators.
/// </summary>
/// <remarks>
/// <para>
/// Event IDs are allocated in the 1800-1899 range to avoid collisions with other Encina modules.
/// </para>
/// <para>
/// This class uses source generators to create optimized logging methods.
/// Excluded from code coverage as the generated code is boilerplate.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public static partial class TenancyLog
{
    /// <summary>
    /// Logs that tenant resolution is starting with the specified strategy.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="strategy">The tenant resolution strategy being used.</param>
    [LoggerMessage(
        EventId = 1800,
        Level = LogLevel.Debug,
        Message = "Resolving tenant using strategy {Strategy}")]
    public static partial void ResolvingTenant(
        ILogger logger,
        string strategy);

    /// <summary>
    /// Logs that a tenant was successfully resolved using the specified strategy.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="tenantId">The identifier of the resolved tenant.</param>
    /// <param name="strategy">The tenant resolution strategy that was used.</param>
    [LoggerMessage(
        EventId = 1801,
        Level = LogLevel.Information,
        Message = "Tenant {TenantId} resolved using {Strategy}")]
    public static partial void TenantResolved(
        ILogger logger,
        string tenantId,
        string strategy);

    /// <summary>
    /// Logs that tenant resolution failed using the specified strategy.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="strategy">The tenant resolution strategy that was attempted.</param>
    /// <param name="errorMessage">The error message describing the failure.</param>
    [LoggerMessage(
        EventId = 1802,
        Level = LogLevel.Warning,
        Message = "Tenant resolution failed using {Strategy}: {ErrorMessage}")]
    public static partial void TenantResolutionFailed(
        ILogger logger,
        string strategy,
        string errorMessage);

    /// <summary>
    /// Logs that a tenant-scoped query is being executed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entityType">The type name of the entity being queried.</param>
    /// <param name="tenantId">The identifier of the tenant scoping the query.</param>
    [LoggerMessage(
        EventId = 1803,
        Level = LogLevel.Debug,
        Message = "Executing tenant-scoped query for {EntityType} (TenantId: {TenantId})")]
    public static partial void ExecutingTenantScopedQuery(
        ILogger logger,
        string entityType,
        string tenantId);

    /// <summary>
    /// Logs that tenant resolution failed with an unexpected exception.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    [LoggerMessage(
        EventId = 1804,
        Level = LogLevel.Error,
        Message = "Tenant resolution failed with unexpected exception")]
    public static partial void TenantResolutionException(
        ILogger logger,
        Exception exception);
}
