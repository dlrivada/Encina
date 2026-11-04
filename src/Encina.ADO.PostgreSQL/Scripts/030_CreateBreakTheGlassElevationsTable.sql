-- =============================================
-- BreakTheGlassElevations - Break-the-glass emergency access
-- One row per justified, time-boxed elevation of a user's access to one data subject's data in one scope.
-- Identifiers are quoted (case-sensitive): BreakTheGlassStoreADO, BreakTheGlassStoreDapper and the
-- EF Core configuration all use the same quoted PascalCase table and columns.
-- Lengths come from Encina.Security.BreakTheGlass.BreakTheGlassLimits.
-- Status: 0 Active, 1 Expired, 2 Revoked. ReviewOutcome: 0 Justified, 1 Unjustified (NULL while unreviewed).
-- Key columns compare case-sensitively: the default deterministic PostgreSQL collation does that.
-- UX_BreakTheGlassElevations_ActiveKey is the atomic arbiter of "one active elevation per tenant, user, subject and scope":
-- a partial unique index over the key (no tenant is the empty string) that covers the Active rows only, so a concurrent
-- second insert fails with SQLSTATE 23505 on that index.
-- =============================================
CREATE TABLE IF NOT EXISTS "BreakTheGlassElevations" (
    "ElevationId"         UUID           NOT NULL PRIMARY KEY,
    "TenantId"            VARCHAR(128)   NULL,
    "UserId"              VARCHAR(256)   NOT NULL,
    "SubjectId"           VARCHAR(256)   NOT NULL,
    "Scope"               VARCHAR(256)   NOT NULL,
    "Justification"       VARCHAR(2000)  NOT NULL,
    "OpenedAtUtc"         TIMESTAMPTZ    NOT NULL,
    "ExpiresAtUtc"        TIMESTAMPTZ    NOT NULL,
    "Status"              INTEGER        NOT NULL,
    "ClosedAtUtc"         TIMESTAMPTZ    NULL,
    "CloseReason"         VARCHAR(256)   NULL,
    "ExpiryRecordedAtUtc" TIMESTAMPTZ    NULL,
    "ReviewOutcome"       INTEGER        NULL,
    "ReviewedByUserId"    VARCHAR(256)   NULL,
    "ReviewedAtUtc"       TIMESTAMPTZ    NULL,
    "ReviewNote"          VARCHAR(2000)  NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_BreakTheGlassElevations_ActiveKey"
    ON "BreakTheGlassElevations" ((COALESCE("TenantId", '')), "UserId", "SubjectId", "Scope")
    WHERE "Status" = 0;
CREATE INDEX IF NOT EXISTS "IX_BreakTheGlassElevations_ReviewQueue" ON "BreakTheGlassElevations" ("TenantId", "ReviewOutcome", "OpenedAtUtc");
CREATE INDEX IF NOT EXISTS "IX_BreakTheGlassElevations_Expiry" ON "BreakTheGlassElevations" ("Status", "ExpiresAtUtc");
