CREATE TABLE [dbo].[DPIAAuditEntries] (
    [Id]            NVARCHAR(36)   NOT NULL PRIMARY KEY,
    [AssessmentId]  NVARCHAR(36)   NOT NULL,
    [Action]        NVARCHAR(256)  NOT NULL,
    [PerformedBy]   NVARCHAR(256)  NULL,
    [OccurredAtUtc] DATETIME2(7)   NOT NULL,
    [Details]       NVARCHAR(MAX)  NULL,
    [TenantId]      NVARCHAR(256)  NULL,
    [ModuleId]      NVARCHAR(256)  NULL,
    INDEX [IX_DPIAAuditEntries_AssessmentId] ([AssessmentId])
);
GO
