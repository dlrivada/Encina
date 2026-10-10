-- =============================================
-- DeadLetterMessages - Dead Letter Queue
-- Messages that failed for good, kept for inspection, replay and expiry.
-- Lengths come from Encina.Messaging.DeadLetter.DeadLetterStoreLimits.
-- =============================================
-- String filter columns use a binary collation so that SourcePattern/SourceMessageId (the unique idempotency key) and the other filter columns compare case-sensitively, like PostgreSQL and MongoDB.
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DeadLetterMessages]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[DeadLetterMessages] (
        [Id]                  UNIQUEIDENTIFIER NOT NULL,
        [RequestType]         NVARCHAR(1000)   COLLATE Latin1_General_100_BIN2 NOT NULL,
        [RequestContent]      NVARCHAR(MAX)    NOT NULL,
        [ErrorCode]           NVARCHAR(256)    COLLATE Latin1_General_100_BIN2 NOT NULL,
        [ExceptionType]       NVARCHAR(512)    NULL,
        [ExceptionStackTrace] NVARCHAR(MAX)    NULL,
        [CorrelationId]       NVARCHAR(256)    COLLATE Latin1_General_100_BIN2 NULL,
        [SourcePattern]       NVARCHAR(64)     COLLATE Latin1_General_100_BIN2 NOT NULL,
        [SourceMessageId]     NVARCHAR(256)    COLLATE Latin1_General_100_BIN2 NOT NULL,
        [TenantId]            NVARCHAR(128)    COLLATE Latin1_General_100_BIN2 NULL,
        [TotalRetryAttempts]  INT              NOT NULL,
        [FirstFailedAtUtc]    DATETIME2(7)     NOT NULL,
        [DeadLetteredAtUtc]   DATETIME2(7)     NOT NULL,
        [ExpiresAtUtc]        DATETIME2(7)     NULL,
        [ReplayClaimedAtUtc]  DATETIME2(7)     NULL,
        [ReplayedAtUtc]       DATETIME2(7)     NULL,
        [ReplayResult]        NVARCHAR(256)    NULL,

        CONSTRAINT [PK_DeadLetterMessages] PRIMARY KEY CLUSTERED ([Id]),

        INDEX [UX_DeadLetterMessages_Source] UNIQUE ([SourcePattern], [SourceMessageId]),
        INDEX [IX_DeadLetterMessages_DeadLetteredAt] ([DeadLetteredAtUtc], [Id]),
        INDEX [IX_DeadLetterMessages_Pending] ([ReplayedAtUtc], [SourcePattern], [DeadLetteredAtUtc]),
        INDEX [IX_DeadLetterMessages_ExpiresAt] ([ExpiresAtUtc]),
        INDEX [IX_DeadLetterMessages_CorrelationId] ([CorrelationId]),
        INDEX [IX_DeadLetterMessages_Tenant] ([TenantId], [DeadLetteredAtUtc])
    );
    PRINT 'Created table: DeadLetterMessages';
END
ELSE
BEGIN
    PRINT 'Table already exists: DeadLetterMessages';
END
GO
