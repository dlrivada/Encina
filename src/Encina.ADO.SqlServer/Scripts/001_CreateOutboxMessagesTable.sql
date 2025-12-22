-- =============================================
-- Create OutboxMessages table
-- For reliable event publishing (at-least-once delivery)
-- =============================================

CREATE TABLE [dbo].[OutboxMessages]
(
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [NotificationType] NVARCHAR(500) NOT NULL,
    [Content] NVARCHAR(MAX) NOT NULL,
    [CreatedAtUtc] DATETIME2(7) NOT NULL,
    [ProcessedAtUtc] DATETIME2(7) NULL,
    [ErrorMessage] NVARCHAR(MAX) NULL,
    [RetryCount] INT NOT NULL DEFAULT 0,
    [NextRetryAtUtc] DATETIME2(7) NULL,

    INDEX [IX_OutboxMessages_ProcessedAt_RetryCount]
        ([ProcessedAtUtc], [RetryCount], [NextRetryAtUtc])
        INCLUDE ([CreatedAtUtc])
);
GO
