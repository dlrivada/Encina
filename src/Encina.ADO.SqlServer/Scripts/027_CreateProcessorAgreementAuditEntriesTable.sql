CREATE TABLE [dbo].[ProcessorAgreementAuditEntries] (
    [Id]                NVARCHAR(256)  NOT NULL PRIMARY KEY,
    [ProcessorId]       NVARCHAR(256)  NOT NULL,
    [DPAId]             NVARCHAR(256)  NULL,
    [Action]            NVARCHAR(256)  NOT NULL,
    [Detail]            NVARCHAR(MAX)  NULL,
    [PerformedByUserId] NVARCHAR(256)  NULL,
    [OccurredAtUtc]     DATETIME2(7)   NOT NULL,
    [TenantId]          NVARCHAR(256)  NULL,
    [ModuleId]          NVARCHAR(256)  NULL,
    INDEX [IX_ProcessorAgreementAuditEntries_ProcessorId] ([ProcessorId])
);
GO
