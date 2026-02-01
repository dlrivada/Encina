-- =============================================
-- Create AuditLogs table for SQL Server
-- For audit trail tracking
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AuditLogs]
    (
        [Id] NVARCHAR(256) NOT NULL,
        [EntityType] NVARCHAR(256) NOT NULL,
        [EntityId] NVARCHAR(256) NOT NULL,
        [Action] INT NOT NULL,
        [UserId] NVARCHAR(256) NULL,
        [TimestampUtc] DATETIME2(7) NOT NULL,
        [OldValues] NVARCHAR(MAX) NULL,
        [NewValues] NVARCHAR(MAX) NULL,
        [CorrelationId] NVARCHAR(256) NULL,

        CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED ([Id]),

        INDEX [IX_AuditLogs_Entity] ([EntityType], [EntityId]),
        INDEX [IX_AuditLogs_Timestamp] ([TimestampUtc]),
        INDEX [IX_AuditLogs_UserId] ([UserId]) WHERE [UserId] IS NOT NULL,
        INDEX [IX_AuditLogs_CorrelationId] ([CorrelationId]) WHERE [CorrelationId] IS NOT NULL
    );
    PRINT 'Created table: AuditLogs';
END
ELSE
BEGIN
    PRINT 'Table already exists: AuditLogs';
END
GO
