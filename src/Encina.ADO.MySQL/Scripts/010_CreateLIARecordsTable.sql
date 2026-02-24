-- Creates the LIARecords table for Legitimate Interest Assessment records.
-- Each row represents a documented balancing test under GDPR Article 6(1)(f).

CREATE TABLE `LIARecords` (
    `Id`                         VARCHAR(256)  NOT NULL PRIMARY KEY,
    `Name`                       VARCHAR(512)  NOT NULL,
    `Purpose`                    VARCHAR(1024) NOT NULL,
    `LegitimateInterest`         TEXT          NOT NULL,
    `Benefits`                   TEXT          NOT NULL,
    `ConsequencesIfNotProcessed` TEXT          NOT NULL,
    `NecessityJustification`     TEXT          NOT NULL,
    `AlternativesConsideredJson` TEXT          NOT NULL,
    `DataMinimisationNotes`      TEXT          NOT NULL,
    `NatureOfData`               TEXT          NOT NULL,
    `ReasonableExpectations`     TEXT          NOT NULL,
    `ImpactAssessment`           TEXT          NOT NULL,
    `SafeguardsJson`             TEXT          NOT NULL,
    `OutcomeValue`               INT           NOT NULL,
    `Conclusion`                 TEXT          NOT NULL,
    `Conditions`                 TEXT          NULL,
    `AssessedAtUtc`              DATETIME(6)   NOT NULL,
    `AssessedBy`                 VARCHAR(256)  NOT NULL,
    `DPOInvolvement`             TINYINT(1)    NOT NULL DEFAULT 0,
    `NextReviewAtUtc`            DATETIME(6)   NULL
);

CREATE INDEX `IX_LIARecords_OutcomeValue` ON `LIARecords`(`OutcomeValue`);
