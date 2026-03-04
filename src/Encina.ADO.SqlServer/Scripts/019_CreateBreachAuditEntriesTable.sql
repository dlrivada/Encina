CREATE TABLE [dbo].[BreachAuditEntries] (
    [Id]                NVARCHAR(36)    NOT NULL PRIMARY KEY,
    [BreachId]          NVARCHAR(36)    NOT NULL,
    [Action]            NVARCHAR(256)   NOT NULL,
    [Detail]            NVARCHAR(MAX)   NULL,
    [PerformedByUserId] NVARCHAR(256)   NULL,
    [OccurredAtUtc]     DATETIME2(7)    NOT NULL,
    INDEX [IX_BreachAuditEntries_BreachId] ([BreachId])
);
GO
