-- =============================================
-- Create OutboxMessages table
-- For reliable event publishing (at-least-once delivery)
-- =============================================

CREATE TABLE OutboxMessages
(
    Id RAW(16) NOT NULL PRIMARY KEY,
    NotificationType VARCHAR2(500) NOT NULL,
    Content CLOB NOT NULL,
    CreatedAtUtc TIMESTAMP NOT NULL,
    ProcessedAtUtc TIMESTAMP NULL,
    ErrorMessage CLOB NULL,
    RetryCount NUMBER(10) DEFAULT 0 NOT NULL,
    NextRetryAtUtc TIMESTAMP NULL
);

CREATE INDEX IX_OutboxMessages_ProcessedAt ON OutboxMessages (ProcessedAtUtc, RetryCount, NextRetryAtUtc);
