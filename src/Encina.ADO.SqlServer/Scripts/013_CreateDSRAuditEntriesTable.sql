-- =============================================
-- Create DSRAuditEntries table
-- For GDPR Data Subject Rights audit trail
-- =============================================

CREATE TABLE [dbo].[DSRAuditEntries]
(
    [Id]                NVARCHAR(36)   NOT NULL PRIMARY KEY,
    [DSRRequestId]      NVARCHAR(36)   NOT NULL,
    [Action]            NVARCHAR(256)  NOT NULL,
    [Detail]            NVARCHAR(MAX)  NULL,
    [PerformedByUserId] NVARCHAR(256)  NULL,
    [OccurredAtUtc]     DATETIME2(7)   NOT NULL,

    INDEX [IX_DSRAuditEntries_DSRRequestId] ([DSRRequestId]),
    INDEX [IX_DSRAuditEntries_OccurredAtUtc] ([OccurredAtUtc])
);
GO
