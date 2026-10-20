-- =============================================
-- DeadLetterMessages - Dead Letter Queue
-- Messages that failed for good, kept for inspection, replay and expiry.
-- Identifiers are quoted (case-sensitive): DeadLetterStoreADO, DeadLetterStoreDapper and the
-- EF Core configuration all use the same quoted PascalCase table and columns.
-- Lengths come from Encina.Messaging.DeadLetter.DeadLetterStoreLimits.
-- =============================================
CREATE TABLE IF NOT EXISTS "DeadLetterMessages" (
    "Id"                  UUID           NOT NULL PRIMARY KEY,
    "RequestType"         VARCHAR(1000)  NOT NULL,
    "RequestContent"      TEXT           NOT NULL,
    "ErrorCode"           VARCHAR(256)   NOT NULL,
    "ExceptionType"       VARCHAR(512)   NULL,
    "ExceptionStackTrace" TEXT           NULL,
    "CorrelationId"       VARCHAR(256)   NULL,
    "SourcePattern"       VARCHAR(64)    NOT NULL,
    "SourceMessageId"     VARCHAR(256)   NOT NULL,
    "TenantId"            VARCHAR(128)   NULL,
    "TotalRetryAttempts"  INTEGER        NOT NULL,
    "FirstFailedAtUtc"    TIMESTAMPTZ    NOT NULL,
    "DeadLetteredAtUtc"   TIMESTAMPTZ    NOT NULL,
    "ExpiresAtUtc"        TIMESTAMPTZ    NULL,
    "ReplayClaimedAtUtc"  TIMESTAMPTZ    NULL,
    "ReplayedAtUtc"       TIMESTAMPTZ    NULL,
    "ReplayResult"        VARCHAR(256)   NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_DeadLetterMessages_Source" ON "DeadLetterMessages" ("SourcePattern", "SourceMessageId");
CREATE INDEX IF NOT EXISTS "IX_DeadLetterMessages_DeadLetteredAt" ON "DeadLetterMessages" ("DeadLetteredAtUtc", "Id");
CREATE INDEX IF NOT EXISTS "IX_DeadLetterMessages_Pending" ON "DeadLetterMessages" ("ReplayedAtUtc", "SourcePattern", "DeadLetteredAtUtc");
CREATE INDEX IF NOT EXISTS "IX_DeadLetterMessages_ExpiresAt" ON "DeadLetterMessages" ("ExpiresAtUtc");
CREATE INDEX IF NOT EXISTS "IX_DeadLetterMessages_CorrelationId" ON "DeadLetterMessages" ("CorrelationId");
CREATE INDEX IF NOT EXISTS "IX_DeadLetterMessages_Tenant" ON "DeadLetterMessages" ("TenantId", "DeadLetteredAtUtc");
