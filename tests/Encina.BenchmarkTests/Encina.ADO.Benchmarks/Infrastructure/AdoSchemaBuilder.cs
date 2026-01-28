using System.Data;

namespace Encina.ADO.Benchmarks.Infrastructure;

/// <summary>
/// Creates database schemas for ADO.NET benchmarks across all supported providers.
/// </summary>
public static class AdoSchemaBuilder
{
    /// <summary>
    /// Creates all benchmark tables for the specified provider.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="provider">The database provider type.</param>
    public static void CreateAllTables(IDbConnection connection, DatabaseProvider provider)
    {
        CreateOutboxTable(connection, provider);
        CreateInboxTable(connection, provider);
        CreateSagaTable(connection, provider);
        CreateScheduledMessageTable(connection, provider);
        CreateBenchmarkEntityTable(connection, provider);
    }

    /// <summary>
    /// Creates the benchmark entity table for repository testing.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="provider">The database provider type.</param>
    public static void CreateBenchmarkEntityTable(IDbConnection connection, DatabaseProvider provider)
    {
        var sql = GetBenchmarkEntityTableSql(provider);
        ExecuteSql(connection, sql);
    }

    /// <summary>
    /// Creates the outbox messages table.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="provider">The database provider type.</param>
    public static void CreateOutboxTable(IDbConnection connection, DatabaseProvider provider)
    {
        var sql = GetOutboxTableSql(provider);
        ExecuteSql(connection, sql);
    }

    /// <summary>
    /// Creates the inbox messages table.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="provider">The database provider type.</param>
    public static void CreateInboxTable(IDbConnection connection, DatabaseProvider provider)
    {
        var sql = GetInboxTableSql(provider);
        ExecuteSql(connection, sql);
    }

    /// <summary>
    /// Creates the saga state table.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="provider">The database provider type.</param>
    public static void CreateSagaTable(IDbConnection connection, DatabaseProvider provider)
    {
        var sql = GetSagaTableSql(provider);
        ExecuteSql(connection, sql);
    }

    /// <summary>
    /// Creates the scheduled messages table.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="provider">The database provider type.</param>
    public static void CreateScheduledMessageTable(IDbConnection connection, DatabaseProvider provider)
    {
        var sql = GetScheduledMessageTableSql(provider);
        ExecuteSql(connection, sql);
    }

    /// <summary>
    /// Drops all benchmark tables.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="provider">The database provider type.</param>
    public static void DropAllTables(IDbConnection connection, DatabaseProvider provider)
    {
        var dropSql = provider switch
        {
            DatabaseProvider.MySql => @"
                DROP TABLE IF EXISTS ScheduledMessages;
                DROP TABLE IF EXISTS SagaStates;
                DROP TABLE IF EXISTS InboxMessages;
                DROP TABLE IF EXISTS OutboxMessages;",
            _ => @"
                DROP TABLE IF EXISTS ScheduledMessages;
                DROP TABLE IF EXISTS SagaStates;
                DROP TABLE IF EXISTS InboxMessages;
                DROP TABLE IF EXISTS OutboxMessages;"
        };

        ExecuteSql(connection, dropSql);
    }

    private static string GetOutboxTableSql(DatabaseProvider provider) => provider switch
    {
        DatabaseProvider.Sqlite => @"
            CREATE TABLE IF NOT EXISTS OutboxMessages (
                Id TEXT PRIMARY KEY,
                NotificationType TEXT NOT NULL,
                Content TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                ProcessedAtUtc TEXT,
                ErrorMessage TEXT,
                RetryCount INTEGER NOT NULL DEFAULT 0,
                NextRetryAtUtc TEXT
            )",
        DatabaseProvider.SqlServer => @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OutboxMessages' AND xtype='U')
            CREATE TABLE OutboxMessages (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                NotificationType NVARCHAR(500) NOT NULL,
                Content NVARCHAR(MAX) NOT NULL,
                CreatedAtUtc DATETIME2 NOT NULL,
                ProcessedAtUtc DATETIME2,
                ErrorMessage NVARCHAR(MAX),
                RetryCount INT NOT NULL DEFAULT 0,
                NextRetryAtUtc DATETIME2
            )",
        DatabaseProvider.PostgreSql => @"
            CREATE TABLE IF NOT EXISTS OutboxMessages (
                Id UUID PRIMARY KEY,
                NotificationType VARCHAR(500) NOT NULL,
                Content TEXT NOT NULL,
                CreatedAtUtc TIMESTAMP NOT NULL,
                ProcessedAtUtc TIMESTAMP,
                ErrorMessage TEXT,
                RetryCount INT NOT NULL DEFAULT 0,
                NextRetryAtUtc TIMESTAMP
            )",
        DatabaseProvider.MySql => @"
            CREATE TABLE IF NOT EXISTS OutboxMessages (
                Id CHAR(36) PRIMARY KEY,
                NotificationType VARCHAR(500) NOT NULL,
                Content TEXT NOT NULL,
                CreatedAtUtc DATETIME(6) NOT NULL,
                ProcessedAtUtc DATETIME(6),
                ErrorMessage TEXT,
                RetryCount INT NOT NULL DEFAULT 0,
                NextRetryAtUtc DATETIME(6)
            )",
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported database provider")
    };

    private static string GetInboxTableSql(DatabaseProvider provider) => provider switch
    {
        DatabaseProvider.Sqlite => @"
            CREATE TABLE IF NOT EXISTS InboxMessages (
                MessageId TEXT PRIMARY KEY,
                RequestType TEXT NOT NULL,
                ReceivedAtUtc TEXT NOT NULL,
                ProcessedAtUtc TEXT,
                ExpiresAtUtc TEXT NOT NULL,
                Response TEXT,
                ErrorMessage TEXT,
                RetryCount INTEGER NOT NULL DEFAULT 0,
                NextRetryAtUtc TEXT,
                Metadata TEXT
            )",
        DatabaseProvider.SqlServer => @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='InboxMessages' AND xtype='U')
            CREATE TABLE InboxMessages (
                MessageId NVARCHAR(450) PRIMARY KEY,
                RequestType NVARCHAR(500) NOT NULL,
                ReceivedAtUtc DATETIME2 NOT NULL,
                ProcessedAtUtc DATETIME2,
                ExpiresAtUtc DATETIME2 NOT NULL,
                Response NVARCHAR(MAX),
                ErrorMessage NVARCHAR(MAX),
                RetryCount INT NOT NULL DEFAULT 0,
                NextRetryAtUtc DATETIME2,
                Metadata NVARCHAR(MAX)
            )",
        DatabaseProvider.PostgreSql => @"
            CREATE TABLE IF NOT EXISTS InboxMessages (
                MessageId VARCHAR(450) PRIMARY KEY,
                RequestType VARCHAR(500) NOT NULL,
                ReceivedAtUtc TIMESTAMP NOT NULL,
                ProcessedAtUtc TIMESTAMP,
                ExpiresAtUtc TIMESTAMP NOT NULL,
                Response TEXT,
                ErrorMessage TEXT,
                RetryCount INT NOT NULL DEFAULT 0,
                NextRetryAtUtc TIMESTAMP,
                Metadata TEXT
            )",
        DatabaseProvider.MySql => @"
            CREATE TABLE IF NOT EXISTS InboxMessages (
                MessageId VARCHAR(450) PRIMARY KEY,
                RequestType VARCHAR(500) NOT NULL,
                ReceivedAtUtc DATETIME(6) NOT NULL,
                ProcessedAtUtc DATETIME(6),
                ExpiresAtUtc DATETIME(6) NOT NULL,
                Response TEXT,
                ErrorMessage TEXT,
                RetryCount INT NOT NULL DEFAULT 0,
                NextRetryAtUtc DATETIME(6),
                Metadata TEXT
            )",
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported database provider")
    };

    private static string GetSagaTableSql(DatabaseProvider provider) => provider switch
    {
        DatabaseProvider.Sqlite => @"
            CREATE TABLE IF NOT EXISTS SagaStates (
                SagaId TEXT PRIMARY KEY,
                SagaType TEXT NOT NULL,
                Data TEXT NOT NULL,
                Status TEXT NOT NULL,
                CurrentStep INTEGER NOT NULL DEFAULT 0,
                StartedAtUtc TEXT NOT NULL,
                CompletedAtUtc TEXT,
                ErrorMessage TEXT,
                LastUpdatedAtUtc TEXT NOT NULL,
                TimeoutAtUtc TEXT
            )",
        DatabaseProvider.SqlServer => @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SagaStates' AND xtype='U')
            CREATE TABLE SagaStates (
                SagaId UNIQUEIDENTIFIER PRIMARY KEY,
                SagaType NVARCHAR(500) NOT NULL,
                Data NVARCHAR(MAX) NOT NULL,
                Status NVARCHAR(100) NOT NULL,
                CurrentStep INT NOT NULL DEFAULT 0,
                StartedAtUtc DATETIME2 NOT NULL,
                CompletedAtUtc DATETIME2,
                ErrorMessage NVARCHAR(MAX),
                LastUpdatedAtUtc DATETIME2 NOT NULL,
                TimeoutAtUtc DATETIME2
            )",
        DatabaseProvider.PostgreSql => @"
            CREATE TABLE IF NOT EXISTS SagaStates (
                SagaId UUID PRIMARY KEY,
                SagaType VARCHAR(500) NOT NULL,
                Data TEXT NOT NULL,
                Status VARCHAR(100) NOT NULL,
                CurrentStep INT NOT NULL DEFAULT 0,
                StartedAtUtc TIMESTAMP NOT NULL,
                CompletedAtUtc TIMESTAMP,
                ErrorMessage TEXT,
                LastUpdatedAtUtc TIMESTAMP NOT NULL,
                TimeoutAtUtc TIMESTAMP
            )",
        DatabaseProvider.MySql => @"
            CREATE TABLE IF NOT EXISTS SagaStates (
                SagaId CHAR(36) PRIMARY KEY,
                SagaType VARCHAR(500) NOT NULL,
                Data TEXT NOT NULL,
                Status VARCHAR(100) NOT NULL,
                CurrentStep INT NOT NULL DEFAULT 0,
                StartedAtUtc DATETIME(6) NOT NULL,
                CompletedAtUtc DATETIME(6),
                ErrorMessage TEXT,
                LastUpdatedAtUtc DATETIME(6) NOT NULL,
                TimeoutAtUtc DATETIME(6)
            )",
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported database provider")
    };

    private static string GetScheduledMessageTableSql(DatabaseProvider provider) => provider switch
    {
        DatabaseProvider.Sqlite => @"
            CREATE TABLE IF NOT EXISTS ScheduledMessages (
                Id TEXT PRIMARY KEY,
                RequestType TEXT NOT NULL,
                Content TEXT NOT NULL,
                ScheduledAtUtc TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                ProcessedAtUtc TEXT,
                ErrorMessage TEXT,
                RetryCount INTEGER NOT NULL DEFAULT 0,
                NextRetryAtUtc TEXT,
                IsRecurring INTEGER NOT NULL DEFAULT 0,
                CronExpression TEXT,
                LastExecutedAtUtc TEXT
            )",
        DatabaseProvider.SqlServer => @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ScheduledMessages' AND xtype='U')
            CREATE TABLE ScheduledMessages (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                RequestType NVARCHAR(500) NOT NULL,
                Content NVARCHAR(MAX) NOT NULL,
                ScheduledAtUtc DATETIME2 NOT NULL,
                CreatedAtUtc DATETIME2 NOT NULL,
                ProcessedAtUtc DATETIME2,
                ErrorMessage NVARCHAR(MAX),
                RetryCount INT NOT NULL DEFAULT 0,
                NextRetryAtUtc DATETIME2,
                IsRecurring BIT NOT NULL DEFAULT 0,
                CronExpression NVARCHAR(100),
                LastExecutedAtUtc DATETIME2
            )",
        DatabaseProvider.PostgreSql => @"
            CREATE TABLE IF NOT EXISTS ScheduledMessages (
                Id UUID PRIMARY KEY,
                RequestType VARCHAR(500) NOT NULL,
                Content TEXT NOT NULL,
                ScheduledAtUtc TIMESTAMP NOT NULL,
                CreatedAtUtc TIMESTAMP NOT NULL,
                ProcessedAtUtc TIMESTAMP,
                ErrorMessage TEXT,
                RetryCount INT NOT NULL DEFAULT 0,
                NextRetryAtUtc TIMESTAMP,
                IsRecurring BOOLEAN NOT NULL DEFAULT FALSE,
                CronExpression VARCHAR(100),
                LastExecutedAtUtc TIMESTAMP
            )",
        DatabaseProvider.MySql => @"
            CREATE TABLE IF NOT EXISTS ScheduledMessages (
                Id CHAR(36) PRIMARY KEY,
                RequestType VARCHAR(500) NOT NULL,
                Content TEXT NOT NULL,
                ScheduledAtUtc DATETIME(6) NOT NULL,
                CreatedAtUtc DATETIME(6) NOT NULL,
                ProcessedAtUtc DATETIME(6),
                ErrorMessage TEXT,
                RetryCount INT NOT NULL DEFAULT 0,
                NextRetryAtUtc DATETIME(6),
                IsRecurring TINYINT(1) NOT NULL DEFAULT 0,
                CronExpression VARCHAR(100),
                LastExecutedAtUtc DATETIME(6)
            )",
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported database provider")
    };

    private static string GetBenchmarkEntityTableSql(DatabaseProvider provider) => provider switch
    {
        DatabaseProvider.Sqlite => @"
            CREATE TABLE IF NOT EXISTS BenchmarkEntities (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Description TEXT,
                Price REAL NOT NULL,
                Quantity INTEGER NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                Category TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                UpdatedAtUtc TEXT
            )",
        DatabaseProvider.SqlServer => @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='BenchmarkEntities' AND xtype='U')
            CREATE TABLE BenchmarkEntities (
                Id UNIQUEIDENTIFIER PRIMARY KEY,
                Name NVARCHAR(200) NOT NULL,
                Description NVARCHAR(MAX),
                Price DECIMAL(18,2) NOT NULL,
                Quantity INT NOT NULL,
                IsActive BIT NOT NULL DEFAULT 1,
                Category NVARCHAR(100) NOT NULL,
                CreatedAtUtc DATETIME2 NOT NULL,
                UpdatedAtUtc DATETIME2
            )",
        DatabaseProvider.PostgreSql => @"
            CREATE TABLE IF NOT EXISTS BenchmarkEntities (
                Id UUID PRIMARY KEY,
                Name VARCHAR(200) NOT NULL,
                Description TEXT,
                Price DECIMAL(18,2) NOT NULL,
                Quantity INT NOT NULL,
                IsActive BOOLEAN NOT NULL DEFAULT TRUE,
                Category VARCHAR(100) NOT NULL,
                CreatedAtUtc TIMESTAMP NOT NULL,
                UpdatedAtUtc TIMESTAMP
            )",
        DatabaseProvider.MySql => @"
            CREATE TABLE IF NOT EXISTS BenchmarkEntities (
                Id CHAR(36) PRIMARY KEY,
                Name VARCHAR(200) NOT NULL,
                Description TEXT,
                Price DECIMAL(18,2) NOT NULL,
                Quantity INT NOT NULL,
                IsActive TINYINT(1) NOT NULL DEFAULT 1,
                Category VARCHAR(100) NOT NULL,
                CreatedAtUtc DATETIME(6) NOT NULL,
                UpdatedAtUtc DATETIME(6)
            )",
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported database provider")
    };

    private static void ExecuteSql(IDbConnection connection, string sql)
    {
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
