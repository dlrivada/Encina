CREATE TABLE IF NOT EXISTS DataProcessingAgreements (
    Id                              TEXT    NOT NULL PRIMARY KEY,
    ProcessorId                     TEXT    NOT NULL,
    StatusValue                     INTEGER NOT NULL,
    SignedAtUtc                     TEXT    NOT NULL,
    ExpiresAtUtc                    TEXT    NULL,
    HasSCCs                         INTEGER NOT NULL,
    ProcessingPurposesJson          TEXT    NOT NULL,
    ProcessOnDocumentedInstructions INTEGER NOT NULL,
    ConfidentialityObligations      INTEGER NOT NULL,
    SecurityMeasures                INTEGER NOT NULL,
    SubProcessorRequirements        INTEGER NOT NULL,
    DataSubjectRightsAssistance     INTEGER NOT NULL,
    ComplianceAssistance            INTEGER NOT NULL,
    DataDeletionOrReturn            INTEGER NOT NULL,
    AuditRights                     INTEGER NOT NULL,
    TenantId                        TEXT    NULL,
    ModuleId                        TEXT    NULL,
    CreatedAtUtc                    TEXT    NOT NULL,
    LastUpdatedAtUtc                TEXT    NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_DataProcessingAgreements_ProcessorId
    ON DataProcessingAgreements (ProcessorId);

CREATE INDEX IF NOT EXISTS IX_DataProcessingAgreements_StatusValue
    ON DataProcessingAgreements (StatusValue);

CREATE INDEX IF NOT EXISTS IX_DataProcessingAgreements_ExpiresAtUtc
    ON DataProcessingAgreements (ExpiresAtUtc);

CREATE INDEX IF NOT EXISTS IX_DataProcessingAgreements_TenantId
    ON DataProcessingAgreements (TenantId);
