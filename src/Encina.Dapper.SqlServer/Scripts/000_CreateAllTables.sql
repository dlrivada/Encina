-- =============================================
-- Encina.Dapper - Complete Database Schema
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

-- =============================================
-- OperationAuditEntries - Operation Audit Trail
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OperationAuditEntries]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[OperationAuditEntries]
    (
        [Id]                 UNIQUEIDENTIFIER NOT NULL,
        [CorrelationId]      NVARCHAR(256)    NOT NULL,
        [UserId]             NVARCHAR(256)    NULL,
        [TenantId]           NVARCHAR(128)    NULL,
        [Action]             NVARCHAR(128)    NOT NULL,
        [EntityType]         NVARCHAR(256)    NOT NULL,
        [EntityId]           NVARCHAR(256)    NULL,
        [Outcome]            INT              NOT NULL,
        [ErrorMessage]       NVARCHAR(2048)   NULL,
        [TimestampUtc]       DATETIME2(7)     NOT NULL,
        [StartedAtUtc]       DATETIMEOFFSET(7) NOT NULL,
        [CompletedAtUtc]     DATETIMEOFFSET(7) NOT NULL,
        [IpAddress]          NVARCHAR(45)     NULL,
        [UserAgent]          NVARCHAR(512)    NULL,
        [RequestPayloadHash] NVARCHAR(64)     NULL,
        [RequestPayload]     NVARCHAR(MAX)    NULL,
        [ResponsePayload]    NVARCHAR(MAX)    NULL,
        [Metadata]           NVARCHAR(MAX)    NULL,

        CONSTRAINT [PK_OperationAuditEntries] PRIMARY KEY CLUSTERED ([Id]),

        INDEX [IX_OperationAuditEntries_Entity] ([EntityType], [EntityId]),
        INDEX [IX_OperationAuditEntries_Timestamp] ([TimestampUtc]),
        INDEX [IX_OperationAuditEntries_Outcome] ([Outcome]),
        INDEX [IX_OperationAuditEntries_UserId] ([UserId]) WHERE [UserId] IS NOT NULL,
        INDEX [IX_OperationAuditEntries_TenantId] ([TenantId]) WHERE [TenantId] IS NOT NULL,
        INDEX [IX_OperationAuditEntries_CorrelationId] ([CorrelationId]),
        INDEX [IX_OperationAuditEntries_Action] ([Action])
    );
    PRINT 'Created table: OperationAuditEntries';
END
ELSE
BEGIN
    PRINT 'Table already exists: OperationAuditEntries';
END
GO

-- =============================================
-- ReadAuditEntries - Read Access Audit Trail
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReadAuditEntries]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[ReadAuditEntries]
    (
        [Id]              UNIQUEIDENTIFIER NOT NULL,
        [EntityType]      NVARCHAR(256)    NOT NULL,
        [EntityId]        NVARCHAR(256)    NULL,
        [UserId]          NVARCHAR(256)    NULL,
        [TenantId]        NVARCHAR(128)    NULL,
        [AccessedAtUtc]   DATETIME2(7)     NOT NULL,
        [CorrelationId]   NVARCHAR(256)    NULL,
        [Purpose]         NVARCHAR(1024)   NULL,
        [AccessMethod]    INT              NOT NULL DEFAULT 0,
        [EntityCount]     INT              NOT NULL DEFAULT 0,
        [Metadata]        NVARCHAR(MAX)    NULL,

        CONSTRAINT [PK_ReadAuditEntries] PRIMARY KEY CLUSTERED ([Id]),

        INDEX [IX_ReadAuditEntries_Entity] ([EntityType], [EntityId]),
        INDEX [IX_ReadAuditEntries_AccessedAt] ([AccessedAtUtc]),
        INDEX [IX_ReadAuditEntries_UserId] ([UserId]) WHERE [UserId] IS NOT NULL,
        INDEX [IX_ReadAuditEntries_TenantId] ([TenantId]) WHERE [TenantId] IS NOT NULL,
        INDEX [IX_ReadAuditEntries_CorrelationId] ([CorrelationId]) WHERE [CorrelationId] IS NOT NULL,
        INDEX [IX_ReadAuditEntries_AccessMethod] ([AccessMethod])
    );
    PRINT 'Created table: ReadAuditEntries';
END
ELSE
BEGIN
    PRINT 'Table already exists: ReadAuditEntries';
END
GO

-- =============================================
-- abac_policy_sets - ABAC Policy Set Storage
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[abac_policy_sets]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[abac_policy_sets]
    (
        [Id] NVARCHAR(256) NOT NULL,
        [Version] NVARCHAR(256) NULL,
        [Description] NVARCHAR(MAX) NULL,
        [PolicyJson] NVARCHAR(MAX) NOT NULL,
        [IsEnabled] BIT NOT NULL DEFAULT 1,
        [Priority] INT NOT NULL DEFAULT 0,
        [CreatedAtUtc] DATETIME2(7) NOT NULL,
        [UpdatedAtUtc] DATETIME2(7) NOT NULL,

        CONSTRAINT [PK_abac_policy_sets] PRIMARY KEY CLUSTERED ([Id]),

        INDEX [IX_abac_policy_sets_IsEnabled_Priority] ([IsEnabled], [Priority])
    );
    PRINT 'Created table: abac_policy_sets';
END
ELSE
BEGIN
    PRINT 'Table already exists: abac_policy_sets';
END
GO

-- =============================================
-- abac_policies - ABAC Standalone Policy Storage
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[abac_policies]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[abac_policies]
    (
        [Id] NVARCHAR(256) NOT NULL,
        [Version] NVARCHAR(256) NULL,
        [Description] NVARCHAR(MAX) NULL,
        [PolicyJson] NVARCHAR(MAX) NOT NULL,
        [IsEnabled] BIT NOT NULL DEFAULT 1,
        [Priority] INT NOT NULL DEFAULT 0,
        [CreatedAtUtc] DATETIME2(7) NOT NULL,
        [UpdatedAtUtc] DATETIME2(7) NOT NULL,

        CONSTRAINT [PK_abac_policies] PRIMARY KEY CLUSTERED ([Id]),

        INDEX [IX_abac_policies_IsEnabled_Priority] ([IsEnabled], [Priority])
    );
    PRINT 'Created table: abac_policies';
END
ELSE
BEGIN
    PRINT 'Table already exists: abac_policies';
END
GO

-- =============================================
-- DeadLetterMessages - Dead Letter Queue
-- Messages that failed for good, kept for inspection, replay and expiry.
-- Lengths come from Encina.Messaging.DeadLetter.DeadLetterStoreLimits.
-- =============================================
-- String filter columns use a binary collation so that SourcePattern/SourceMessageId (the unique idempotency key) and the other filter columns compare case-sensitively, like PostgreSQL and MongoDB.
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DeadLetterMessages]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[DeadLetterMessages] (
        [Id]                  UNIQUEIDENTIFIER NOT NULL,
        [RequestType]         NVARCHAR(1000)   COLLATE Latin1_General_100_BIN2 NOT NULL,
        [RequestContent]      NVARCHAR(MAX)    NOT NULL,
        [ErrorCode]           NVARCHAR(256)    COLLATE Latin1_General_100_BIN2 NOT NULL,
        [ExceptionType]       NVARCHAR(512)    NULL,
        [ExceptionStackTrace] NVARCHAR(MAX)    NULL,
        [CorrelationId]       NVARCHAR(256)    COLLATE Latin1_General_100_BIN2 NULL,
        [SourcePattern]       NVARCHAR(64)     COLLATE Latin1_General_100_BIN2 NOT NULL,
        [SourceMessageId]     NVARCHAR(256)    COLLATE Latin1_General_100_BIN2 NOT NULL,
        [TenantId]            NVARCHAR(128)    COLLATE Latin1_General_100_BIN2 NULL,
        [TotalRetryAttempts]  INT              NOT NULL,
        [FirstFailedAtUtc]    DATETIME2(7)     NOT NULL,
        [DeadLetteredAtUtc]   DATETIME2(7)     NOT NULL,
        [ExpiresAtUtc]        DATETIME2(7)     NULL,
        [ReplayClaimedAtUtc]  DATETIME2(7)     NULL,
        [ReplayedAtUtc]       DATETIME2(7)     NULL,
        [ReplayResult]        NVARCHAR(256)    NULL,

        CONSTRAINT [PK_DeadLetterMessages] PRIMARY KEY CLUSTERED ([Id]),

        INDEX [UX_DeadLetterMessages_Source] UNIQUE ([SourcePattern], [SourceMessageId]),
        INDEX [IX_DeadLetterMessages_DeadLetteredAt] ([DeadLetteredAtUtc], [Id]),
        INDEX [IX_DeadLetterMessages_Pending] ([ReplayedAtUtc], [SourcePattern], [DeadLetteredAtUtc]),
        INDEX [IX_DeadLetterMessages_ExpiresAt] ([ExpiresAtUtc]),
        INDEX [IX_DeadLetterMessages_CorrelationId] ([CorrelationId]),
        INDEX [IX_DeadLetterMessages_Tenant] ([TenantId], [DeadLetteredAtUtc])
    );
    PRINT 'Created table: DeadLetterMessages';
END
ELSE
BEGIN
    PRINT 'Table already exists: DeadLetterMessages';
END
GO

PRINT '';
PRINT 'Encina.Dapper schema installation complete!';
PRINT 'You can now use all messaging patterns with Dapper.';
GO
