-- =============================================
-- Create ScheduledMessages table
-- For delayed and recurring command execution
-- =============================================

CREATE TABLE [dbo].[ScheduledMessages]
(
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [RequestType] NVARCHAR(500) NOT NULL,
    [Content] NVARCHAR(MAX) NOT NULL,
    [ScheduledAtUtc] DATETIME2(7) NOT NULL,
    [CreatedAtUtc] DATETIME2(7) NOT NULL,
    [ProcessedAtUtc] DATETIME2(7) NULL,
    [LastExecutedAtUtc] DATETIME2(7) NULL,
    [ErrorMessage] NVARCHAR(MAX) NULL,
    [RetryCount] INT NOT NULL DEFAULT 0,
    [NextRetryAtUtc] DATETIME2(7) NULL,
    [IsRecurring] BIT NOT NULL DEFAULT 0,
    [CronExpression] NVARCHAR(100) NULL,

    INDEX [IX_ScheduledMessages_ScheduledAt_Processed]
        ([ScheduledAtUtc], [ProcessedAtUtc], [RetryCount])
        INCLUDE ([NextRetryAtUtc], [IsRecurring])
);
GO
