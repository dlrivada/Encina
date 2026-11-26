using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.DomainModeling.Diagnostics;

/// <summary>
/// High-performance logging methods for Unit of Work operations using LoggerMessage source generators.
/// </summary>
/// <remarks>
/// <para>
/// Event IDs are allocated in the 10200-10299 range to avoid collisions with other Encina modules.
/// </para>
/// <para>
/// This class uses source generators to create optimized logging methods.
/// Excluded from code coverage as the generated code is boilerplate.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public static partial class UnitOfWorkLog
{
    /// <summary>Logs when SaveChanges begins execution.</summary>
    [LoggerMessage(
        EventId = 10200,
        Level = LogLevel.Debug,
        Message = "Starting SaveChanges")]
    public static partial void StartingSaveChanges(ILogger logger);

    /// <summary>Logs when SaveChanges completes successfully.</summary>
    [LoggerMessage(
        EventId = 10201,
        Level = LogLevel.Information,
        Message = "SaveChanges completed with {AffectedRows} affected rows")]
    public static partial void SaveChangesCompleted(
        ILogger logger,
        int affectedRows);

    /// <summary>Logs when SaveChanges fails with a domain error.</summary>
    [LoggerMessage(
        EventId = 10202,
        Level = LogLevel.Warning,
        Message = "SaveChanges failed: {ErrorMessage}")]
    public static partial void SaveChangesFailed(
        ILogger logger,
        string errorMessage);

    /// <summary>Logs when a transaction begins.</summary>
    [LoggerMessage(
        EventId = 10203,
        Level = LogLevel.Debug,
        Message = "Starting transaction")]
    public static partial void StartingTransaction(ILogger logger);

    /// <summary>Logs when a transaction completes with a given outcome.</summary>
    [LoggerMessage(
        EventId = 10204,
        Level = LogLevel.Information,
        Message = "Transaction {Outcome}")]
    public static partial void TransactionCompleted(
        ILogger logger,
        string outcome);

    /// <summary>Logs when a transaction fails with an unexpected exception.</summary>
    [LoggerMessage(
        EventId = 10205,
        Level = LogLevel.Error,
        Message = "Transaction failed with unexpected exception")]
    public static partial void TransactionException(
        ILogger logger,
        Exception exception);
}
