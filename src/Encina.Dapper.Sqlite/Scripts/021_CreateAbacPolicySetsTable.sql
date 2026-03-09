-- =============================================
-- Create abac_policy_sets table for SQLite
-- ABAC policy set storage with JSON policy graph
-- =============================================

CREATE TABLE IF NOT EXISTS "abac_policy_sets"
(
    "Id" TEXT NOT NULL,
    "Version" TEXT NULL,
    "Description" TEXT NULL,
    "PolicyJson" TEXT NOT NULL,
    "IsEnabled" INTEGER NOT NULL DEFAULT 1,
    "Priority" INTEGER NOT NULL DEFAULT 0,
    "CreatedAtUtc" TEXT NOT NULL,
    "UpdatedAtUtc" TEXT NOT NULL,

    PRIMARY KEY ("Id")
);

-- Index for filtering enabled policy sets ordered by priority
CREATE INDEX IF NOT EXISTS "IX_abac_policy_sets_IsEnabled_Priority"
    ON "abac_policy_sets" ("IsEnabled", "Priority");
