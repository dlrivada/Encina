CREATE TABLE IF NOT EXISTS BreachPhasedReports (
    Id                TEXT    NOT NULL PRIMARY KEY,
    BreachId          TEXT    NOT NULL REFERENCES BreachRecords(Id),
    ReportNumber      INTEGER NOT NULL,
    Content           TEXT    NOT NULL,
    SubmittedAtUtc    TEXT    NOT NULL,
    SubmittedByUserId TEXT    NULL
);

CREATE INDEX IF NOT EXISTS IX_BreachPhasedReports_BreachId ON BreachPhasedReports (BreachId);
