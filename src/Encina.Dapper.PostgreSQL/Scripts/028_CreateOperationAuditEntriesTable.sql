-- Identifiers are quoted (case-sensitive): OperationAuditStoreADO and OperationAuditStoreDapper
-- quote every table and column name.
CREATE TABLE IF NOT EXISTS "OperationAuditEntries" (
    "Id"                 UUID           NOT NULL PRIMARY KEY,
    "CorrelationId"      VARCHAR(256)   NOT NULL,
    "UserId"             VARCHAR(256)   NULL,
    "TenantId"           VARCHAR(128)   NULL,
    "Action"             VARCHAR(128)   NOT NULL,
    "EntityType"         VARCHAR(256)   NOT NULL,
    "EntityId"           VARCHAR(256)   NULL,
    "Outcome"            INTEGER        NOT NULL,
    "ErrorMessage"       VARCHAR(2048)  NULL,
    "TimestampUtc"       TIMESTAMPTZ    NOT NULL,
    "StartedAtUtc"       TIMESTAMPTZ    NOT NULL,
    "CompletedAtUtc"     TIMESTAMPTZ    NOT NULL,
    "IpAddress"          VARCHAR(45)    NULL,
    "UserAgent"          VARCHAR(512)   NULL,
    "RequestPayloadHash" VARCHAR(64)    NULL,
    "RequestPayload"     TEXT           NULL,
    "ResponsePayload"    TEXT           NULL,
    "Metadata"           TEXT           NULL
);

CREATE INDEX IF NOT EXISTS "IX_OperationAuditEntries_Entity" ON "OperationAuditEntries" ("EntityType", "EntityId");
CREATE INDEX IF NOT EXISTS "IX_OperationAuditEntries_Timestamp" ON "OperationAuditEntries" ("TimestampUtc");
CREATE INDEX IF NOT EXISTS "IX_OperationAuditEntries_Outcome" ON "OperationAuditEntries" ("Outcome");
CREATE INDEX IF NOT EXISTS "IX_OperationAuditEntries_UserId" ON "OperationAuditEntries" ("UserId") WHERE "UserId" IS NOT NULL;
CREATE INDEX IF NOT EXISTS "IX_OperationAuditEntries_TenantId" ON "OperationAuditEntries" ("TenantId") WHERE "TenantId" IS NOT NULL;
CREATE INDEX IF NOT EXISTS "IX_OperationAuditEntries_CorrelationId" ON "OperationAuditEntries" ("CorrelationId");
CREATE INDEX IF NOT EXISTS "IX_OperationAuditEntries_Action" ON "OperationAuditEntries" ("Action");
