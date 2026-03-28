-- =============================================
-- Create OutboxMessages table
-- For reliable event publishing (at-least-once delivery)
-- =============================================

CREATE TABLE IF NOT EXISTS OutboxMessages
(
    Id TEXT NOT NULL PRIMARY KEY,
    NotificationType TEXT NOT NULL,
    Content TEXT NOT NULL,
    CreatedAtUtc TEXT NOT NULL,
    ProcessedAtUtc TEXT NULL,
    ErrorMessage TEXT NULL,
    RetryCount INTEGER NOT NULL DEFAULT 0,
    NextRetryAtUtc TEXT NULL
);

CREATE INDEX IF NOT EXISTS IX_OutboxMessages_ProcessedAt
    ON OutboxMessages (ProcessedAtUtc, RetryCount, NextRetryAtUtc);
