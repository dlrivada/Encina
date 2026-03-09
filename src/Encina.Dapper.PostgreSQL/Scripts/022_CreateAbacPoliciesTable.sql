-- =============================================
-- Create abac_policies table for PostgreSQL
-- ABAC standalone policy storage with JSONB policy graph
-- =============================================

CREATE TABLE IF NOT EXISTS "abac_policies"
(
    "Id" VARCHAR(256) NOT NULL,
    "Version" VARCHAR(256) NULL,
    "Description" TEXT NULL,
    "PolicyJson" JSONB NOT NULL,
    "IsEnabled" BOOLEAN NOT NULL DEFAULT TRUE,
    "Priority" INTEGER NOT NULL DEFAULT 0,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL,
    "UpdatedAtUtc" TIMESTAMPTZ NOT NULL,

    CONSTRAINT "PK_abac_policies" PRIMARY KEY ("Id")
);

-- Index for filtering enabled policies ordered by priority
CREATE INDEX IF NOT EXISTS "IX_abac_policies_IsEnabled_Priority"
    ON "abac_policies" ("IsEnabled", "Priority");
