-- =============================================
-- Create InboxMessages table
-- For idempotent message processing (exactly-once semantics)
-- =============================================

CREATE TABLE InboxMessages
(
    MessageId VARCHAR2(255) NOT NULL PRIMARY KEY,
    RequestType VARCHAR2(500) NOT NULL,
    ReceivedAtUtc TIMESTAMP NOT NULL,
    ProcessedAtUtc TIMESTAMP NULL,
    ExpiresAtUtc TIMESTAMP NOT NULL,
    Response CLOB NULL,
    ErrorMessage CLOB NULL,
    RetryCount NUMBER(10) DEFAULT 0 NOT NULL,
    NextRetryAtUtc TIMESTAMP NULL,
    Metadata CLOB NULL
);

CREATE INDEX IX_InboxMessages_ExpiresAt ON InboxMessages (ExpiresAtUtc);
