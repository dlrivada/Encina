CREATE TABLE IF NOT EXISTS BreachRecords (
    Id                                TEXT    NOT NULL PRIMARY KEY,
    Nature                            TEXT    NOT NULL,
    ApproximateSubjectsAffected       INTEGER NOT NULL,
    CategoriesOfDataAffected          TEXT    NOT NULL,
    DPOContactDetails                 TEXT    NOT NULL,
    LikelyConsequences                TEXT    NOT NULL,
    MeasuresTaken                     TEXT    NOT NULL,
    DetectedAtUtc                     TEXT    NOT NULL,
    NotificationDeadlineUtc           TEXT    NOT NULL,
    NotifiedAuthorityAtUtc            TEXT    NULL,
    NotifiedSubjectsAtUtc             TEXT    NULL,
    SeverityValue                     INTEGER NOT NULL,
    StatusValue                       INTEGER NOT NULL,
    DelayReason                       TEXT    NULL,
    SubjectNotificationExemptionValue INTEGER NOT NULL,
    ResolvedAtUtc                     TEXT    NULL,
    ResolutionSummary                 TEXT    NULL
);

CREATE INDEX IF NOT EXISTS IX_BreachRecords_StatusValue ON BreachRecords (StatusValue);
CREATE INDEX IF NOT EXISTS IX_BreachRecords_DetectedAtUtc ON BreachRecords (DetectedAtUtc);
