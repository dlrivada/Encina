-- Creates the ProcessingActivities table for GDPR Article 30 Records of Processing Activities (RoPA).
-- Each row represents a registered processing activity linked to an Encina request type.

CREATE TABLE IF NOT EXISTS ProcessingActivities
(
    Id                             TEXT    NOT NULL PRIMARY KEY,
    RequestTypeName                TEXT    NOT NULL,
    Name                           TEXT    NOT NULL,
    Purpose                        TEXT    NOT NULL,
    LawfulBasisValue               INTEGER NOT NULL,
    CategoriesOfDataSubjectsJson   TEXT    NOT NULL,
    CategoriesOfPersonalDataJson   TEXT    NOT NULL,
    RecipientsJson                 TEXT    NOT NULL,
    ThirdCountryTransfers          TEXT    NULL,
    Safeguards                     TEXT    NULL,
    RetentionPeriodTicks           INTEGER NOT NULL,
    SecurityMeasures               TEXT    NOT NULL,
    CreatedAtUtc                   TEXT    NOT NULL,
    LastUpdatedAtUtc               TEXT    NOT NULL,
    CONSTRAINT UQ_ProcessingActivities_RequestTypeName UNIQUE (RequestTypeName)
);

CREATE INDEX IF NOT EXISTS IX_ProcessingActivities_LawfulBasisValue ON ProcessingActivities (LawfulBasisValue);
CREATE INDEX IF NOT EXISTS IX_ProcessingActivities_CreatedAtUtc ON ProcessingActivities (CreatedAtUtc);
