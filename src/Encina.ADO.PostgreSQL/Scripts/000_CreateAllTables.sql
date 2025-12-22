-- =============================================
-- SimpleMediator.Dapper - Complete Database Schema
-- Run this script to create all messaging pattern tables
-- =============================================

-- =============================================
-- OutboxMessages - Reliable Event Publishing
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OutboxMessages]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[OutboxMessages]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [NotificationType] NVARCHAR(500) NOT NULL,
        [Content] NVARCHAR(MAX) NOT NULL,
        [CreatedAtUtc] DATETIME2(7) NOT NULL,
        [ProcessedAtUtc] DATETIME2(7) NULL,
        [ErrorMessage] NVARCHAR(MAX) NULL,
        [RetryCount] INT NOT NULL DEFAULT 0,
        [NextRetryAtUtc] DATETIME2(7) NULL,

        INDEX [IX_OutboxMessages_ProcessedAt_RetryCount]
            ([ProcessedAtUtc], [RetryCount], [NextRetryAtUtc])
            INCLUDE ([CreatedAtUtc])
    );
    PRINT 'Created table: OutboxMessages';
END
ELSE
BEGIN
    PRINT 'Table already exists: OutboxMessages';
END
GO

-- =============================================
-- InboxMessages - Idempotent Message Processing
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InboxMessages]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[InboxMessages]
    (
        [MessageId] NVARCHAR(255) NOT NULL PRIMARY KEY,
        [RequestType] NVARCHAR(500) NOT NULL,
        [ReceivedAtUtc] DATETIME2(7) NOT NULL,
        [ProcessedAtUtc] DATETIME2(7) NULL,
        [ExpiresAtUtc] DATETIME2(7) NOT NULL,
        [Response] NVARCHAR(MAX) NULL,
        [ErrorMessage] NVARCHAR(MAX) NULL,
        [RetryCount] INT NOT NULL DEFAULT 0,
        [NextRetryAtUtc] DATETIME2(7) NULL,
        [Metadata] NVARCHAR(MAX) NULL,

        INDEX [IX_InboxMessages_ExpiresAt]
            ([ExpiresAtUtc])
            WHERE [ProcessedAtUtc] IS NOT NULL
    );
    PRINT 'Created table: InboxMessages';
END
ELSE
BEGIN
    PRINT 'Table already exists: InboxMessages';
END
GO

-- =============================================
-- SagaStates - Distributed Transaction Orchestration
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SagaStates]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SagaStates]
    (
        [SagaId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [SagaType] NVARCHAR(500) NOT NULL,
        [Data] NVARCHAR(MAX) NOT NULL,
        [Status] INT NOT NULL, -- 0=Running, 1=Completed, 2=Failed, 3=Compensating, 4=Compensated
        [StartedAtUtc] DATETIME2(7) NOT NULL,
        [LastUpdatedAtUtc] DATETIME2(7) NOT NULL,
        [CompletedAtUtc] DATETIME2(7) NULL,
        [ErrorMessage] NVARCHAR(MAX) NULL,
        [CurrentStep] INT NOT NULL DEFAULT 0,

        INDEX [IX_SagaStates_Status_LastUpdated]
            ([Status], [LastUpdatedAtUtc])
    );
    PRINT 'Created table: SagaStates';
END
ELSE
BEGIN
    PRINT 'Table already exists: SagaStates';
END
GO

-- =============================================
-- ScheduledMessages - Delayed/Recurring Execution
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ScheduledMessages]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ScheduledMessages]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [RequestType] NVARCHAR(500) NOT NULL,
        [Content] NVARCHAR(MAX) NOT NULL,
        [ScheduledAtUtc] DATETIME2(7) NOT NULL,
        [CreatedAtUtc] DATETIME2(7) NOT NULL,
        [ProcessedAtUtc] DATETIME2(7) NULL,
        [LastExecutedAtUtc] DATETIME2(7) NULL,
        [ErrorMessage] NVARCHAR(MAX) NULL,
        [RetryCount] INT NOT NULL DEFAULT 0,
        [NextRetryAtUtc] DATETIME2(7) NULL,
        [IsRecurring] BIT NOT NULL DEFAULT 0,
        [CronExpression] NVARCHAR(100) NULL,

        INDEX [IX_ScheduledMessages_ScheduledAt_Processed]
            ([ScheduledAtUtc], [ProcessedAtUtc], [RetryCount])
            INCLUDE ([NextRetryAtUtc], [IsRecurring])
    );
    PRINT 'Created table: ScheduledMessages';
END
ELSE
BEGIN
    PRINT 'Table already exists: ScheduledMessages';
END
GO

PRINT '';
PRINT 'SimpleMediator.Dapper schema installation complete!';
PRINT 'You can now use all messaging patterns with Dapper.';
GO
