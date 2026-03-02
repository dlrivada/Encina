CREATE TABLE [dbo].[ResidencyAuditEntries] (
    [Id]           NVARCHAR(36)    NOT NULL PRIMARY KEY,
    [EntityId]     NVARCHAR(256)   NULL,
    [DataCategory] NVARCHAR(256)   NOT NULL,
    [SourceRegion] NVARCHAR(32)    NOT NULL,
    [TargetRegion] NVARCHAR(32)    NULL,
    [ActionValue]  INT             NOT NULL,
    [OutcomeValue] INT             NOT NULL,
    [LegalBasis]   NVARCHAR(256)   NULL,
    [RequestType]  NVARCHAR(512)   NULL,
    [UserId]       NVARCHAR(256)   NULL,
    [TimestampUtc] DATETIME2(7)    NOT NULL,
    [Details]      NVARCHAR(MAX)   NULL,
    INDEX [IX_ResidencyAuditEntries_EntityId] ([EntityId]),
    INDEX [IX_ResidencyAuditEntries_TimestampUtc] ([TimestampUtc]),
    INDEX [IX_ResidencyAuditEntries_OutcomeValue] ([OutcomeValue])
);
GO
