CREATE TABLE [dbo].[BreachRecords] (
    [Id]                                NVARCHAR(36)    NOT NULL PRIMARY KEY,
    [Nature]                            NVARCHAR(MAX)   NOT NULL,
    [ApproximateSubjectsAffected]       INT             NOT NULL,
    [CategoriesOfDataAffected]          NVARCHAR(MAX)   NOT NULL,
    [DPOContactDetails]                 NVARCHAR(1024)  NOT NULL,
    [LikelyConsequences]                NVARCHAR(MAX)   NOT NULL,
    [MeasuresTaken]                     NVARCHAR(MAX)   NOT NULL,
    [DetectedAtUtc]                     DATETIME2(7)    NOT NULL,
    [NotificationDeadlineUtc]           DATETIME2(7)    NOT NULL,
    [NotifiedAuthorityAtUtc]            DATETIME2(7)    NULL,
    [NotifiedSubjectsAtUtc]             DATETIME2(7)    NULL,
    [SeverityValue]                     INT             NOT NULL,
    [StatusValue]                       INT             NOT NULL,
    [DelayReason]                       NVARCHAR(MAX)   NULL,
    [SubjectNotificationExemptionValue] INT             NOT NULL,
    [ResolvedAtUtc]                     DATETIME2(7)    NULL,
    [ResolutionSummary]                 NVARCHAR(MAX)   NULL,
    INDEX [IX_BreachRecords_StatusValue] ([StatusValue]),
    INDEX [IX_BreachRecords_DetectedAtUtc] ([DetectedAtUtc])
);
GO
