-- =============================================
-- Create DSRRequests table
-- For GDPR Data Subject Rights (Articles 15-22)
-- =============================================

CREATE TABLE [dbo].[DSRRequests]
(
    [Id]                    NVARCHAR(36)    NOT NULL PRIMARY KEY,
    [SubjectId]             NVARCHAR(256)   NOT NULL,
    [RightTypeValue]        INT             NOT NULL,
    [StatusValue]           INT             NOT NULL,
    [ReceivedAtUtc]         DATETIME2(7)    NOT NULL,
    [DeadlineAtUtc]         DATETIME2(7)    NOT NULL,
    [CompletedAtUtc]        DATETIME2(7)    NULL,
    [ExtensionReason]       NVARCHAR(1024)  NULL,
    [ExtendedDeadlineAtUtc] DATETIME2(7)    NULL,
    [RejectionReason]       NVARCHAR(1024)  NULL,
    [RequestDetails]        NVARCHAR(MAX)   NULL,
    [VerifiedAtUtc]         DATETIME2(7)    NULL,
    [ProcessedByUserId]     NVARCHAR(256)   NULL,

    INDEX [IX_DSRRequests_SubjectId] ([SubjectId]),
    INDEX [IX_DSRRequests_StatusValue] ([StatusValue]),
    INDEX [IX_DSRRequests_RightTypeValue] ([RightTypeValue]),
    INDEX [IX_DSRRequests_DeadlineAtUtc] ([DeadlineAtUtc])
);
GO
