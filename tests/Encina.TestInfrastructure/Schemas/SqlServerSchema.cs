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
                    TimeoutAtUtc DATETIME2 NULL,
                    CorrelationId NVARCHAR(256) NULL,
                    Metadata NVARCHAR(MAX) NULL
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
    /// Creates the Orders table schema for immutable update integration tests.
    /// </summary>
    public static async Task CreateOrdersSchemaAsync(SqlConnection connection, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DROP TABLE IF EXISTS Orders;
            CREATE TABLE Orders (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    CustomerName NVARCHAR(200) NOT NULL,
                    Status NVARCHAR(50) NOT NULL
                );
            """;

        await ExecuteInTransactionAsync(connection, sql, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates the TestRepositoryEntities table schema for repository integration tests.
    /// Also creates Table-Valued Parameter (TVP) types for SQL Server bulk operations.
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

            -- TVP types for BulkOperations (drop existing types first)
            IF TYPE_ID('dbo.TestRepositoryEntitiesType_Ids') IS NOT NULL
                DROP TYPE dbo.TestRepositoryEntitiesType_Ids;
            IF TYPE_ID('dbo.TestRepositoryEntitiesType_Update') IS NOT NULL
                DROP TYPE dbo.TestRepositoryEntitiesType_Update;
            IF TYPE_ID('dbo.TestRepositoryEntitiesType_Merge') IS NOT NULL
                DROP TYPE dbo.TestRepositoryEntitiesType_Merge;

            -- TVP type for BulkDelete and BulkRead (just Id column)
            CREATE TYPE dbo.TestRepositoryEntitiesType_Ids AS TABLE (
                Id UNIQUEIDENTIFIER NOT NULL
            );

            -- TVP type for BulkUpdate (PK first, then remaining columns alphabetically to match EF Core)
            CREATE TYPE dbo.TestRepositoryEntitiesType_Update AS TABLE (
                Id UNIQUEIDENTIFIER NOT NULL,
                Amount DECIMAL(18,2) NOT NULL,
                CreatedAtUtc DATETIME2 NOT NULL,
                IsActive BIT NOT NULL,
                Name NVARCHAR(200) NOT NULL
            );

            -- TVP type for BulkMerge (PK first, then remaining columns alphabetically to match EF Core)
            CREATE TYPE dbo.TestRepositoryEntitiesType_Merge AS TABLE (
                Id UNIQUEIDENTIFIER NOT NULL,
                Amount DECIMAL(18,2) NOT NULL,
                CreatedAtUtc DATETIME2 NOT NULL,
                IsActive BIT NOT NULL,
                Name NVARCHAR(200) NOT NULL
            );

            DROP TABLE IF EXISTS TestEntities;
            CREATE TABLE TestEntities (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    Name NVARCHAR(200) NOT NULL,
                    Amount DECIMAL(18,2) NOT NULL,
                    IsActive BIT NOT NULL,
                    CreatedAtUtc DATETIME2 NOT NULL
                );

            CREATE INDEX IX_TestEntities_IsActive
                ON TestEntities(IsActive);

            DROP TABLE IF EXISTS ImmutableAggregates;
            CREATE TABLE ImmutableAggregates (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    Name NVARCHAR(200) NOT NULL,
                    Amount DECIMAL(18,2) NOT NULL
                );
            """;

        await ExecuteInTransactionAsync(connection, sql, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates the TenantTestEntities table schema for multi-tenancy integration tests.
    /// </summary>
    public static async Task CreateTenantTestSchemaAsync(SqlConnection connection, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DROP TABLE IF EXISTS TenantTestEntities;
            CREATE TABLE TenantTestEntities (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    TenantId NVARCHAR(128) NOT NULL,
                    Name NVARCHAR(200) NOT NULL,
                    Description NVARCHAR(1000) NULL,
                    Amount DECIMAL(18,2) NOT NULL,
                    IsActive BIT NOT NULL DEFAULT 1,
                    CreatedAtUtc DATETIME2 NOT NULL,
                    UpdatedAtUtc DATETIME2 NULL
                );

            CREATE INDEX IX_TenantTestEntities_TenantId
                ON TenantTestEntities(TenantId);

            CREATE INDEX IX_TenantTestEntities_TenantId_IsActive
                ON TenantTestEntities(TenantId, IsActive);

            CREATE INDEX IX_TenantTestEntities_CreatedAtUtc
                ON TenantTestEntities(CreatedAtUtc);
            """;

        await ExecuteInTransactionAsync(connection, sql, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates the ReadWriteTestEntities table schema for read/write separation tests.
    /// </summary>
    public static async Task CreateReadWriteTestSchemaAsync(SqlConnection connection, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DROP TABLE IF EXISTS ReadWriteTestEntities;
            CREATE TABLE ReadWriteTestEntities (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    Name NVARCHAR(256) NOT NULL,
                    Value INT NOT NULL,
                    Timestamp DATETIME2 NOT NULL,
                    WriteCounter INT NOT NULL DEFAULT 0
                );

            CREATE INDEX IX_ReadWriteTestEntities_Timestamp
                ON ReadWriteTestEntities(Timestamp);
            """;

        await ExecuteInTransactionAsync(connection, sql, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates the ConsentRecords table schema for consent management integration tests.
    /// </summary>
    public static async Task CreateConsentSchemaAsync(SqlConnection connection, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DROP TABLE IF EXISTS ConsentRecords;
            CREATE TABLE ConsentRecords (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    SubjectId NVARCHAR(256) NOT NULL,
                    Purpose NVARCHAR(256) NOT NULL,
                    Status INT NOT NULL,
                    ConsentVersionId NVARCHAR(256) NOT NULL,
                    GivenAtUtc DATETIMEOFFSET(7) NOT NULL,
                    WithdrawnAtUtc DATETIMEOFFSET(7) NULL,
                    ExpiresAtUtc DATETIMEOFFSET(7) NULL,
                    Source NVARCHAR(256) NOT NULL,
                    IpAddress NVARCHAR(45) NULL,
                    ProofOfConsent NVARCHAR(MAX) NULL,
                    Metadata NVARCHAR(MAX) NULL
                );

            CREATE INDEX IX_ConsentRecords_SubjectId
                ON ConsentRecords(SubjectId);

            CREATE INDEX IX_ConsentRecords_SubjectId_Purpose
                ON ConsentRecords(SubjectId, Purpose);

            CREATE INDEX IX_ConsentRecords_Status
                ON ConsentRecords(Status);
            """;

        await ExecuteInTransactionAsync(connection, sql, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates the LawfulBasisRegistrations and LIARecords table schemas for lawful basis integration tests.
    /// </summary>
    public static async Task CreateLawfulBasisSchemaAsync(SqlConnection connection, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DROP TABLE IF EXISTS LIARecords;
            DROP TABLE IF EXISTS LawfulBasisRegistrations;

            CREATE TABLE LawfulBasisRegistrations (
                    Id NVARCHAR(450) NOT NULL PRIMARY KEY,
                    RequestTypeName NVARCHAR(450) NOT NULL UNIQUE,
                    BasisValue INT NOT NULL,
                    Purpose NVARCHAR(MAX) NULL,
                    LIAReference NVARCHAR(MAX) NULL,
                    LegalReference NVARCHAR(MAX) NULL,
                    ContractReference NVARCHAR(MAX) NULL,
                    RegisteredAtUtc DATETIMEOFFSET(7) NOT NULL
                );

            CREATE INDEX IX_LawfulBasisRegistrations_RequestTypeName
                ON LawfulBasisRegistrations(RequestTypeName);

            CREATE TABLE LIARecords (
                    Id NVARCHAR(450) NOT NULL PRIMARY KEY,
                    Name NVARCHAR(450) NOT NULL,
                    Purpose NVARCHAR(MAX) NOT NULL,
                    LegitimateInterest NVARCHAR(MAX) NOT NULL,
                    Benefits NVARCHAR(MAX) NOT NULL,
                    ConsequencesIfNotProcessed NVARCHAR(MAX) NOT NULL,
                    NecessityJustification NVARCHAR(MAX) NOT NULL,
                    AlternativesConsideredJson NVARCHAR(MAX) NOT NULL,
                    DataMinimisationNotes NVARCHAR(MAX) NOT NULL,
                    NatureOfData NVARCHAR(MAX) NOT NULL,
                    ReasonableExpectations NVARCHAR(MAX) NOT NULL,
                    ImpactAssessment NVARCHAR(MAX) NOT NULL,
                    SafeguardsJson NVARCHAR(MAX) NOT NULL,
                    OutcomeValue INT NOT NULL,
                    Conclusion NVARCHAR(MAX) NOT NULL,
                    Conditions NVARCHAR(MAX) NULL,
                    AssessedAtUtc DATETIMEOFFSET(7) NOT NULL,
                    AssessedBy NVARCHAR(450) NOT NULL,
                    DPOInvolvement BIT NOT NULL,
                    NextReviewAtUtc DATETIMEOFFSET(7) NULL
                );

            CREATE INDEX IX_LIARecords_NextReviewAtUtc
                ON LIARecords(NextReviewAtUtc);

            CREATE INDEX IX_LIARecords_OutcomeValue
                ON LIARecords(OutcomeValue);
            """;

        await ExecuteInTransactionAsync(connection, sql, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops all Encina tables.
    /// </summary>
    public static async Task DropAllSchemasAsync(SqlConnection connection, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DROP TABLE IF EXISTS LIARecords;
            DROP TABLE IF EXISTS LawfulBasisRegistrations;
            DROP TABLE IF EXISTS ConsentRecords;
            DROP TABLE IF EXISTS TenantTestEntities;
            DROP TABLE IF EXISTS ReadWriteTestEntities;
            DROP TABLE IF EXISTS Orders;
            DROP TABLE IF EXISTS TestRepositoryEntities;
            DROP TABLE IF EXISTS TestEntities;
            DROP TABLE IF EXISTS ImmutableAggregates;
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
            IF OBJECT_ID('LIARecords', 'U') IS NOT NULL DELETE FROM LIARecords;
            IF OBJECT_ID('LawfulBasisRegistrations', 'U') IS NOT NULL DELETE FROM LawfulBasisRegistrations;
            IF OBJECT_ID('TenantTestEntities', 'U') IS NOT NULL DELETE FROM TenantTestEntities;
            IF OBJECT_ID('ReadWriteTestEntities', 'U') IS NOT NULL DELETE FROM ReadWriteTestEntities;
            IF OBJECT_ID('Orders', 'U') IS NOT NULL DELETE FROM Orders;
            IF OBJECT_ID('TestRepositoryEntities', 'U') IS NOT NULL DELETE FROM TestRepositoryEntities;
            IF OBJECT_ID('ConsentRecords', 'U') IS NOT NULL DELETE FROM ConsentRecords;
            IF OBJECT_ID('TestEntities', 'U') IS NOT NULL DELETE FROM TestEntities;
            IF OBJECT_ID('ImmutableAggregates', 'U') IS NOT NULL DELETE FROM ImmutableAggregates;
            IF OBJECT_ID('ScheduledMessages', 'U') IS NOT NULL DELETE FROM ScheduledMessages;
            IF OBJECT_ID('SagaStates', 'U') IS NOT NULL DELETE FROM SagaStates;
            IF OBJECT_ID('InboxMessages', 'U') IS NOT NULL DELETE FROM InboxMessages;
            IF OBJECT_ID('OutboxMessages', 'U') IS NOT NULL DELETE FROM OutboxMessages;
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
