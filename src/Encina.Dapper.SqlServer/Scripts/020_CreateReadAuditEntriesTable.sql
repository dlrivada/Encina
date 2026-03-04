IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReadAuditEntries]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[ReadAuditEntries] (
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
