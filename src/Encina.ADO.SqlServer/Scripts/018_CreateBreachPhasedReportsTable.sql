CREATE TABLE [dbo].[BreachPhasedReports] (
    [Id]                NVARCHAR(36)    NOT NULL PRIMARY KEY,
    [BreachId]          NVARCHAR(36)    NOT NULL,
    [ReportNumber]      INT             NOT NULL,
    [Content]           NVARCHAR(MAX)   NOT NULL,
    [SubmittedAtUtc]    DATETIME2(7)    NOT NULL,
    [SubmittedByUserId] NVARCHAR(256)   NULL,
    INDEX [IX_BreachPhasedReports_BreachId] ([BreachId]),
    CONSTRAINT [FK_BreachPhasedReports_BreachRecords] FOREIGN KEY ([BreachId]) REFERENCES [dbo].[BreachRecords]([Id])
);
GO
