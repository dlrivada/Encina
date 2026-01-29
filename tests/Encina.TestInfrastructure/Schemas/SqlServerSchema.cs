using System.Data;
using System.Runtime.ExceptionServices;

using Microsoft.Data.SqlClient;

namespace Encina.TestInfrastructure.Schemas;

/// <summary>
/// SQL Server schema creation for Encina test databases.
/// </summary>
public static class SqlServerSchema
{
    /// <summary>
    /// Creates the Outbox table schema.
    /// </summary>
    public static async Task CreateOutboxSchemaAsync(SqlConnection connection, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DROP TABLE IF EXISTS OutboxMessages;
            CREATE TABLE OutboxMessages (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    NotificationType NVARCHAR(500) NOT NULL,
                    Content NVARCHAR(MAX) NOT NULL,
                    CreatedAtUtc DATETIME2 NOT NULL,
                    ProcessedAtUtc DATETIME2 NULL,
                    ErrorMessage NVARCHAR(MAX) NULL,
                    RetryCount INT NOT NULL DEFAULT 0,
                    NextRetryAtUtc DATETIME2 NULL
                );

            CREATE INDEX IX_OutboxMessages_ProcessedAtUtc_NextRetryAtUtc
                ON OutboxMessages(ProcessedAtUtc, NextRetryAtUtc);
            """;

        await ExecuteInTransactionAsync(connection, sql, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates the Inbox table schema.
    /// </summary>
    public static async Task CreateInboxSchemaAsync(SqlConnection connection, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DROP TABLE IF EXISTS InboxMessages;
            CREATE TABLE InboxMessages (
                    MessageId NVARCHAR(256) PRIMARY KEY,
                    RequestType NVARCHAR(500) NOT NULL,
                    ReceivedAtUtc DATETIME2 NOT NULL,
                    ProcessedAtUtc DATETIME2 NULL,
                    Response NVARCHAR(MAX) NULL,
                    ErrorMessage NVARCHAR(MAX) NULL,
                    RetryCount INT NOT NULL DEFAULT 0,
                    NextRetryAtUtc DATETIME2 NULL,
                    ExpiresAtUtc DATETIME2 NOT NULL,
                    Metadata NVARCHAR(MAX) NULL CONSTRAINT CK_InboxMessages_Metadata_Json CHECK (Metadata IS NULL OR ISJSON(Metadata) = 1)
                );

            CREATE INDEX IX_InboxMessages_ExpiresAtUtc
                ON InboxMessages(ExpiresAtUtc);
            """;

        await ExecuteInTransactionAsync(connection, sql, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates the Saga table schema.
    /// </summary>
    public static async Task CreateSagaSchemaAsync(SqlConnection connection, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DROP TABLE IF EXISTS SagaStates;
            CREATE TABLE SagaStates (
                    SagaId UNIQUEIDENTIFIER PRIMARY KEY,
                    SagaType NVARCHAR(500) NOT NULL,
                    CurrentStep INT NOT NULL,
                    Status NVARCHAR(50) NOT NULL,
                    Data NVARCHAR(MAX) NOT NULL,
                    StartedAtUtc DATETIME2 NOT NULL,
                    LastUpdatedAtUtc DATETIME2 NOT NULL,
                    CompletedAtUtc DATETIME2 NULL,
                    ErrorMessage NVARCHAR(MAX) NULL,
                    TimeoutAtUtc DATETIME2 NULL
                );

            CREATE INDEX IX_SagaStates_Status_LastUpdatedAtUtc
                ON SagaStates(Status, LastUpdatedAtUtc);
            """;

        await ExecuteInTransactionAsync(connection, sql, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates the Scheduling table schema.
    /// </summary>
    public static async Task CreateSchedulingSchemaAsync(SqlConnection connection, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DROP TABLE IF EXISTS ScheduledMessages;
            CREATE TABLE ScheduledMessages (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    RequestType NVARCHAR(500) NOT NULL,
                    Content NVARCHAR(MAX) NOT NULL,
                    ScheduledAtUtc DATETIME2 NOT NULL,
                    CreatedAtUtc DATETIME2 NOT NULL,
                    ProcessedAtUtc DATETIME2 NULL,
                    ErrorMessage NVARCHAR(MAX) NULL,
                    RetryCount INT NOT NULL DEFAULT 0,
                    NextRetryAtUtc DATETIME2 NULL,
                    CorrelationId NVARCHAR(256) NULL,
                    Metadata NVARCHAR(MAX) NULL,
                    IsRecurring BIT NOT NULL DEFAULT 0,
                    CronExpression NVARCHAR(200) NULL,
                    LastExecutedAtUtc DATETIME2 NULL
                );

            CREATE INDEX IX_ScheduledMessages_ScheduledAtUtc_ProcessedAtUtc
                ON ScheduledMessages(ScheduledAtUtc, ProcessedAtUtc);
            """;

        await ExecuteInTransactionAsync(connection, sql, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates the TestRepositoryEntities table schema for repository integration tests.
    /// </summary>
    public static async Task CreateRepositoryTestSchemaAsync(SqlConnection connection, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DROP TABLE IF EXISTS TestRepositoryEntities;
            CREATE TABLE TestRepositoryEntities (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    Name NVARCHAR(200) NOT NULL,
                    Amount DECIMAL(18,2) NOT NULL,
                    IsActive BIT NOT NULL,
                    CreatedAtUtc DATETIME2 NOT NULL
                );

            CREATE INDEX IX_TestRepositoryEntities_IsActive
                ON TestRepositoryEntities(IsActive);
            """;

        await ExecuteInTransactionAsync(connection, sql, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops all Encina tables.
    /// </summary>
    public static async Task DropAllSchemasAsync(SqlConnection connection, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DROP TABLE IF EXISTS TestRepositoryEntities;
            DROP TABLE IF EXISTS ScheduledMessages;
            DROP TABLE IF EXISTS SagaStates;
            DROP TABLE IF EXISTS InboxMessages;
            DROP TABLE IF EXISTS OutboxMessages;
            """;

        await ExecuteInTransactionAsync(connection, sql, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Clears all data from Encina tables without dropping schemas.
    /// Useful for cleaning between tests that share a database fixture.
    /// </summary>
    public static async Task ClearAllDataAsync(SqlConnection connection, CancellationToken cancellationToken = default)
    {
        const string sql = """
            IF OBJECT_ID('TestRepositoryEntities', 'U') IS NOT NULL DELETE FROM TestRepositoryEntities;
            DELETE FROM ScheduledMessages;
            DELETE FROM SagaStates;
            DELETE FROM InboxMessages;
            DELETE FROM OutboxMessages;
            """;

        await ExecuteInTransactionAsync(connection, sql, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes SQL within a transaction, committing on success or rolling back on failure.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="connection"/> is not open.</exception>
    private static async Task ExecuteInTransactionAsync(SqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException(
                $"Connection must be open before beginning a transaction. Current state: {connection.State}");
        }

        SqlTransaction transaction;
        try
        {
            transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            // TOCTOU: connection state may have changed between the check and BeginTransactionAsync
            throw new InvalidOperationException(
                $"Failed to begin transaction. Connection state at failure: {connection.State}. " +
                "The connection may have been closed or broken after the initial state check.",
                ex);
        }

        await using (transaction)
            try
            {
                await using var command = new SqlCommand(sql, connection, transaction);
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await TryRollbackAsync(transaction, ex).ConfigureAwait(false);
            }
    }

    /// <summary>
    /// Default timeout for rollback operations.
    /// </summary>
    private static readonly TimeSpan DefaultRollbackTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Attempts to rollback the transaction, preserving the original exception.
    /// If rollback fails or times out, throws an <see cref="AggregateException"/> containing both errors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="SqlServerSchema.TryRollbackAsync"/> does not accept a <see cref="CancellationToken"/>
    /// because rollback must be attempted even when the original operation was cancelled. This ensures
    /// the database remains in a consistent state after cancellation or failure.
    /// </para>
    /// <para>
    /// The rollback operation uses <see cref="Task.WaitAsync(TimeSpan)"/> with a configurable timeout
    /// (default 30 seconds) to prevent indefinite hangs. If the rollback times out, a
    /// <see cref="TimeoutException"/> is included in the <see cref="AggregateException"/>.
    /// </para>
    /// </remarks>
    /// <param name="transaction">The SQL transaction to rollback.</param>
    /// <param name="originalException">The exception that triggered the rollback attempt.</param>
    /// <param name="timeout">Optional timeout for the rollback operation. Defaults to 30 seconds.</param>
    private static async Task TryRollbackAsync(SqlTransaction transaction, Exception originalException, TimeSpan? timeout = null)
    {
        var rollbackTimeout = timeout ?? DefaultRollbackTimeout;

        try
        {
            await transaction.RollbackAsync().WaitAsync(rollbackTimeout).ConfigureAwait(false);
        }
        catch (TimeoutException timeoutException)
        {
            // Rollback timed out - include timeout as the rollback failure
            throw new AggregateException(
                $"Transaction failed and rollback timed out after {rollbackTimeout.TotalSeconds} seconds.",
                originalException,
                timeoutException);
        }
        catch (Exception rollbackException)
        {
            // Preserve both the original exception and the rollback failure
            throw new AggregateException("Transaction failed and rollback also failed.", originalException, rollbackException);
        }

        // Rollback succeeded, rethrow original exception preserving stack trace
        ExceptionDispatchInfo.Capture(originalException).Throw();
    }
}
