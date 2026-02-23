-- =============================================
-- Create ConsentVersions table for SQLite
-- For consent version tracking and reconsent management
-- =============================================

CREATE TABLE IF NOT EXISTS ConsentVersions
(
    VersionId TEXT NOT NULL PRIMARY KEY,
    Purpose TEXT NOT NULL,
    EffectiveFromUtc TEXT NOT NULL,
    Description TEXT NOT NULL,
    RequiresExplicitReconsent INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS IX_ConsentVersions_Purpose ON ConsentVersions (Purpose);
CREATE INDEX IF NOT EXISTS IX_ConsentVersions_Purpose_EffectiveFromUtc ON ConsentVersions (Purpose, EffectiveFromUtc);
