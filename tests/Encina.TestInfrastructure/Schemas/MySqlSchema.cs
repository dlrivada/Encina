using MySqlConnector;

namespace Encina.TestInfrastructure.Schemas;

/// <summary>
/// MySQL schema creation for Encina test databases.
/// </summary>
public static class MySqlSchema
{
    /// <summary>
    /// Creates the Outbox table schema.
    /// </summary>
    public static async Task CreateOutboxSchemaAsync(MySqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS OutboxMessages (
                Id CHAR(36) PRIMARY KEY,
                NotificationType VARCHAR(500) NOT NULL,
                Content TEXT NOT NULL,
                CreatedAtUtc DATETIME(6) NOT NULL,
                ProcessedAtUtc DATETIME(6) NULL,
                ErrorMessage TEXT NULL,
                RetryCount INT NOT NULL DEFAULT 0,
                NextRetryAtUtc DATETIME(6) NULL,
                INDEX IX_OutboxMessages_ProcessedAtUtc_NextRetryAtUtc (ProcessedAtUtc, NextRetryAtUtc)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """;

        using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Inbox table schema.
    /// </summary>
    public static async Task CreateInboxSchemaAsync(MySqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS InboxMessages (
                MessageId VARCHAR(256) PRIMARY KEY,
                RequestType VARCHAR(500) NOT NULL,
                ReceivedAtUtc DATETIME(6) NOT NULL,
                ProcessedAtUtc DATETIME(6) NULL,
                Response TEXT NULL,
                ErrorMessage TEXT NULL,
                RetryCount INT NOT NULL DEFAULT 0,
                NextRetryAtUtc DATETIME(6) NULL,
                ExpiresAtUtc DATETIME(6) NOT NULL,
                Metadata TEXT NULL,
                INDEX IX_InboxMessages_ExpiresAtUtc (ExpiresAtUtc)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """;

        using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Saga table schema.
    /// </summary>
    public static async Task CreateSagaSchemaAsync(MySqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS SagaStates (
                SagaId CHAR(36) PRIMARY KEY,
                SagaType VARCHAR(500) NOT NULL,
                CurrentStep INT NOT NULL,
                Status VARCHAR(50) NOT NULL,
                Data TEXT NOT NULL,
                StartedAtUtc DATETIME(6) NOT NULL,
                LastUpdatedAtUtc DATETIME(6) NOT NULL,
                CompletedAtUtc DATETIME(6) NULL,
                ErrorMessage TEXT NULL,
                TimeoutAtUtc DATETIME(6) NULL,
                CorrelationId VARCHAR(256) NULL,
                Metadata TEXT NULL,
                INDEX IX_SagaStates_Status_LastUpdatedAtUtc (Status, LastUpdatedAtUtc)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """;

        using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Scheduling table schema.
    /// </summary>
    public static async Task CreateSchedulingSchemaAsync(MySqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS ScheduledMessages (
                Id CHAR(36) PRIMARY KEY,
                RequestType VARCHAR(500) NOT NULL,
                Content TEXT NOT NULL,
                ScheduledAtUtc DATETIME(6) NOT NULL,
                CreatedAtUtc DATETIME(6) NOT NULL,
                ProcessedAtUtc DATETIME(6) NULL,
                LastExecutedAtUtc DATETIME(6) NULL,
                ErrorMessage TEXT NULL,
                RetryCount INT NOT NULL DEFAULT 0,
                NextRetryAtUtc DATETIME(6) NULL,
                CorrelationId VARCHAR(256) NULL,
                Metadata TEXT NULL,
                IsRecurring TINYINT(1) NOT NULL DEFAULT 0,
                CronExpression VARCHAR(200) NULL,
                INDEX IX_ScheduledMessages_ScheduledAtUtc_ProcessedAtUtc (ScheduledAtUtc, ProcessedAtUtc)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """;

        using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Orders table schema for immutable update integration tests.
    /// </summary>
    public static async Task CreateOrdersSchemaAsync(MySqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS `Orders` (
                `Id` CHAR(36) PRIMARY KEY,
                `CustomerName` VARCHAR(200) NOT NULL,
                `Status` VARCHAR(50) NOT NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """;

        using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the TestRepositoryEntities table schema for repository integration tests.
    /// </summary>
    public static async Task CreateTestRepositorySchemaAsync(MySqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS `TestRepositoryEntities` (
                `Id` CHAR(36) PRIMARY KEY,
                `Name` VARCHAR(200) NOT NULL,
                `Amount` DECIMAL(18,2) NOT NULL,
                `IsActive` TINYINT(1) NOT NULL,
                `CreatedAtUtc` DATETIME(6) NOT NULL,
                INDEX `IX_TestRepositoryEntities_IsActive` (`IsActive`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """;

        using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the TenantTestEntities table schema for multi-tenancy integration tests.
    /// </summary>
    public static async Task CreateTenantTestSchemaAsync(MySqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS `TenantTestEntities` (
                `Id` CHAR(36) PRIMARY KEY,
                `TenantId` VARCHAR(128) NOT NULL,
                `Name` VARCHAR(200) NOT NULL,
                `Description` VARCHAR(1000) NULL,
                `Amount` DECIMAL(18,2) NOT NULL,
                `IsActive` TINYINT(1) NOT NULL,
                `CreatedAtUtc` DATETIME(6) NOT NULL,
                `UpdatedAtUtc` DATETIME(6) NULL,
                INDEX `IX_TenantTestEntities_TenantId` (`TenantId`),
                INDEX `IX_TenantTestEntities_TenantId_IsActive` (`TenantId`, `IsActive`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """;

        using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the ReadWriteTestEntities table schema for read/write separation tests.
    /// </summary>
    public static async Task CreateReadWriteTestSchemaAsync(MySqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS `ReadWriteTestEntities` (
                `Id` CHAR(36) PRIMARY KEY,
                `Name` VARCHAR(256) NOT NULL,
                `Value` INT NOT NULL,
                `Timestamp` DATETIME(6) NOT NULL,
                `WriteCounter` INT NOT NULL DEFAULT 0,
                INDEX `IX_ReadWriteTestEntities_Timestamp` (`Timestamp`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """;

        using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the ConsentRecords table schema for consent management integration tests.
    /// </summary>
    public static async Task CreateConsentSchemaAsync(MySqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS ConsentRecords (
                Id CHAR(36) PRIMARY KEY,
                SubjectId VARCHAR(256) NOT NULL,
                Purpose VARCHAR(256) NOT NULL,
                Status INT NOT NULL,
                ConsentVersionId VARCHAR(256) NOT NULL,
                GivenAtUtc DATETIME(6) NOT NULL,
                WithdrawnAtUtc DATETIME(6) NULL,
                ExpiresAtUtc DATETIME(6) NULL,
                Source VARCHAR(256) NOT NULL,
                IpAddress VARCHAR(45) NULL,
                ProofOfConsent TEXT NULL,
                Metadata TEXT NULL,
                INDEX IX_ConsentRecords_SubjectId (SubjectId),
                INDEX IX_ConsentRecords_SubjectId_Purpose (SubjectId, Purpose),
                INDEX IX_ConsentRecords_Status (Status)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """;

        using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the LawfulBasisRegistrations and LIARecords table schemas for lawful basis integration tests.
    /// </summary>
    public static async Task CreateLawfulBasisSchemaAsync(MySqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS LawfulBasisRegistrations (
                Id VARCHAR(450) NOT NULL PRIMARY KEY,
                RequestTypeName VARCHAR(450) NOT NULL UNIQUE,
                BasisValue INT NOT NULL,
                Purpose TEXT NULL,
                LIAReference TEXT NULL,
                LegalReference TEXT NULL,
                ContractReference TEXT NULL,
                RegisteredAtUtc DATETIME(6) NOT NULL,
                INDEX IX_LawfulBasisRegistrations_RequestTypeName (RequestTypeName)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            CREATE TABLE IF NOT EXISTS LIARecords (
                Id VARCHAR(450) NOT NULL PRIMARY KEY,
                Name VARCHAR(450) NOT NULL,
                Purpose TEXT NOT NULL,
                LegitimateInterest TEXT NOT NULL,
                Benefits TEXT NOT NULL,
                ConsequencesIfNotProcessed TEXT NOT NULL,
                NecessityJustification TEXT NOT NULL,
                AlternativesConsideredJson TEXT NOT NULL,
                DataMinimisationNotes TEXT NOT NULL,
                NatureOfData TEXT NOT NULL,
                ReasonableExpectations TEXT NOT NULL,
                ImpactAssessment TEXT NOT NULL,
                SafeguardsJson TEXT NOT NULL,
                OutcomeValue INT NOT NULL,
                Conclusion TEXT NOT NULL,
                Conditions TEXT NULL,
                AssessedAtUtc DATETIME(6) NOT NULL,
                AssessedBy VARCHAR(450) NOT NULL,
                DPOInvolvement TINYINT(1) NOT NULL,
                NextReviewAtUtc DATETIME(6) NULL,
                INDEX IX_LIARecords_NextReviewAtUtc (NextReviewAtUtc),
                INDEX IX_LIARecords_OutcomeValue (OutcomeValue)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """;

        using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Drops all Encina tables.
    /// </summary>
    public static async Task DropAllSchemasAsync(MySqlConnection connection)
    {
        const string sql = """
            DROP TABLE IF EXISTS LIARecords;
            DROP TABLE IF EXISTS LawfulBasisRegistrations;
            DROP TABLE IF EXISTS ConsentRecords;
            DROP TABLE IF EXISTS `TenantTestEntities`;
            DROP TABLE IF EXISTS `ReadWriteTestEntities`;
            DROP TABLE IF EXISTS `Orders`;
            DROP TABLE IF EXISTS `TestRepositoryEntities`;
            DROP TABLE IF EXISTS ScheduledMessages;
            DROP TABLE IF EXISTS SagaStates;
            DROP TABLE IF EXISTS InboxMessages;
            DROP TABLE IF EXISTS OutboxMessages;
            """;

        using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Clears all data from Encina tables without dropping schemas.
    /// Useful for cleaning between tests that share a database fixture.
    /// </summary>
    public static async Task ClearAllDataAsync(MySqlConnection connection)
    {
        const string sql = """
            DELETE FROM LIARecords;
            DELETE FROM LawfulBasisRegistrations;
            DELETE FROM ConsentRecords;
            DELETE FROM `TenantTestEntities`;
            DELETE FROM `ReadWriteTestEntities`;
            DELETE FROM `Orders`;
            DELETE FROM `TestRepositoryEntities`;
            DELETE FROM ScheduledMessages;
            DELETE FROM SagaStates;
            DELETE FROM InboxMessages;
            DELETE FROM OutboxMessages;
            """;

        using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
