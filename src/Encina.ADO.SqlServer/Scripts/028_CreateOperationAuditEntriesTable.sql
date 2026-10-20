IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OperationAuditEntries]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[OperationAuditEntries] (
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
