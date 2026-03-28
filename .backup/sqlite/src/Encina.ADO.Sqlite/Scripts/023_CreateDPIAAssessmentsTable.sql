CREATE TABLE IF NOT EXISTS DPIAAssessments (
    Id                TEXT    NOT NULL PRIMARY KEY,
    RequestTypeName   TEXT    NOT NULL,
    StatusValue       INTEGER NOT NULL,
    ProcessingType    TEXT    NULL,
    Reason            TEXT    NULL,
    ResultJson        TEXT    NULL,
    DPOConsultationJson TEXT  NULL,
    CreatedAtUtc      TEXT    NOT NULL,
    ApprovedAtUtc     TEXT    NULL,
    NextReviewAtUtc   TEXT    NULL,
    TenantId          TEXT    NULL,
    ModuleId          TEXT    NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS UX_DPIAAssessments_RequestTypeName
    ON DPIAAssessments (RequestTypeName);

CREATE INDEX IF NOT EXISTS IX_DPIAAssessments_StatusValue
    ON DPIAAssessments (StatusValue);

CREATE INDEX IF NOT EXISTS IX_DPIAAssessments_NextReviewAtUtc
    ON DPIAAssessments (NextReviewAtUtc);
