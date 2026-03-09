-- =============================================
-- Create abac_policy_sets table for PostgreSQL
-- ABAC policy set storage with JSONB policy graph
-- =============================================

CREATE TABLE IF NOT EXISTS "abac_policy_sets"
(
    "Id" VARCHAR(256) NOT NULL,
    "Version" VARCHAR(256) NULL,
    "Description" TEXT NULL,
    "PolicyJson" JSONB NOT NULL,
    "IsEnabled" BOOLEAN NOT NULL DEFAULT TRUE,
    "Priority" INTEGER NOT NULL DEFAULT 0,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL,
    "UpdatedAtUtc" TIMESTAMPTZ NOT NULL,

    CONSTRAINT "PK_abac_policy_sets" PRIMARY KEY ("Id")
);

-- Index for filtering enabled policy sets ordered by priority
CREATE INDEX IF NOT EXISTS "IX_abac_policy_sets_IsEnabled_Priority"
    ON "abac_policy_sets" ("IsEnabled", "Priority");
