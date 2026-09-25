-- =============================================
-- Create OutboxMessages table
-- For reliable event publishing (at-least-once delivery)
-- =============================================

CREATE TABLE IF NOT EXISTS outboxmessages
(
    id UUID NOT NULL PRIMARY KEY,
    notificationtype TEXT NOT NULL,
    content TEXT NOT NULL,
    createdatutc TIMESTAMP NOT NULL,
    processedatutc TIMESTAMP NULL,
    errormessage TEXT NULL,
    retrycount INTEGER NOT NULL DEFAULT 0,
    nextretryatutc TIMESTAMP NULL
);

CREATE INDEX IF NOT EXISTS ix_outboxmessages_processedat_retrycount
    ON outboxmessages (processedatutc, retrycount, nextretryatutc)
    INCLUDE (createdatutc);
