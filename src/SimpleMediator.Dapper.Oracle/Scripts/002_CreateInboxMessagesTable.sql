-- =============================================
-- Create InboxMessages table
-- For idempotent message processing (exactly-once semantics)
-- =============================================

CREATE TABLE [dbo].[InboxMessages]
(
    [MessageId] NVARCHAR(255) NOT NULL PRIMARY KEY,
    [RequestType] NVARCHAR(500) NOT NULL,
    [ReceivedAtUtc] DATETIME2(7) NOT NULL,
    [ProcessedAtUtc] DATETIME2(7) NULL,
    [ExpiresAtUtc] DATETIME2(7) NOT NULL,
    [Response] NVARCHAR(MAX) NULL,
    [ErrorMessage] NVARCHAR(MAX) NULL,
    [RetryCount] INT NOT NULL DEFAULT 0,
    [NextRetryAtUtc] DATETIME2(7) NULL,
    [Metadata] NVARCHAR(MAX) NULL,

    INDEX [IX_InboxMessages_ExpiresAt]
        ([ExpiresAtUtc])
        WHERE [ProcessedAtUtc] IS NOT NULL
);
GO
