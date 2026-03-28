-- =============================================
-- Create abac_policies table for SQLite
-- ABAC standalone policy storage with JSON policy graph
-- =============================================

CREATE TABLE IF NOT EXISTS "abac_policies"
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

-- Index for filtering enabled policies ordered by priority
CREATE INDEX IF NOT EXISTS "IX_abac_policies_IsEnabled_Priority"
    ON "abac_policies" ("IsEnabled", "Priority");
