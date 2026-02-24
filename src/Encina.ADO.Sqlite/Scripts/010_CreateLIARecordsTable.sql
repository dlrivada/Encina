-- Creates the LIARecords table for Legitimate Interest Assessment records.
-- Each row represents a documented balancing test under GDPR Article 6(1)(f).

CREATE TABLE IF NOT EXISTS LIARecords
(
    Id                         TEXT NOT NULL PRIMARY KEY,
    Name                       TEXT NOT NULL,
    Purpose                    TEXT NOT NULL,
    LegitimateInterest         TEXT NOT NULL,
    Benefits                   TEXT NOT NULL,
    ConsequencesIfNotProcessed TEXT NOT NULL,
    NecessityJustification     TEXT NOT NULL,
    AlternativesConsideredJson TEXT NOT NULL,
    DataMinimisationNotes      TEXT NOT NULL,
    NatureOfData               TEXT NOT NULL,
    ReasonableExpectations     TEXT NOT NULL,
    ImpactAssessment           TEXT NOT NULL,
    SafeguardsJson             TEXT NOT NULL,
    OutcomeValue               INTEGER NOT NULL,
    Conclusion                 TEXT NOT NULL,
    Conditions                 TEXT NULL,
    AssessedAtUtc              TEXT NOT NULL,
    AssessedBy                 TEXT NOT NULL,
    DPOInvolvement             INTEGER NOT NULL DEFAULT 0,
    NextReviewAtUtc            TEXT NULL
);

CREATE INDEX IF NOT EXISTS IX_LIARecords_OutcomeValue ON LIARecords (OutcomeValue);
