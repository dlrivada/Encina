-- =============================================
-- Create ConsentAuditEntries table
-- For GDPR consent audit trail (Article 7(1))
-- =============================================

CREATE TABLE [dbo].[ConsentAuditEntries]
(
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [SubjectId] NVARCHAR(256) NOT NULL,
    [Purpose] NVARCHAR(256) NOT NULL,
    [Action] INT NOT NULL,
    [OccurredAtUtc] DATETIME2(7) NOT NULL,
    [PerformedBy] NVARCHAR(256) NOT NULL,
    [IpAddress] NVARCHAR(45) NULL,
    [Metadata] NVARCHAR(MAX) NOT NULL DEFAULT '{}',

    INDEX [IX_ConsentAuditEntries_SubjectId] ([SubjectId]),
    INDEX [IX_ConsentAuditEntries_SubjectId_Purpose] ([SubjectId], [Purpose]),
    INDEX [IX_ConsentAuditEntries_OccurredAtUtc] ([OccurredAtUtc])
);
GO
