-- Creates the ProcessingActivities table for GDPR Article 30 Records of Processing Activities (RoPA).
-- Each row represents a registered processing activity linked to an Encina request type.

CREATE TABLE `ProcessingActivities` (
    `Id`                             VARCHAR(36)   NOT NULL PRIMARY KEY,
    `RequestTypeName`                VARCHAR(1000) NOT NULL,
    `Name`                           VARCHAR(500)  NOT NULL,
    `Purpose`                        TEXT          NOT NULL,
    `LawfulBasisValue`               INT           NOT NULL,
    `CategoriesOfDataSubjectsJson`   TEXT          NOT NULL,
    `CategoriesOfPersonalDataJson`   TEXT          NOT NULL,
    `RecipientsJson`                 TEXT          NOT NULL,
    `ThirdCountryTransfers`          TEXT          NULL,
    `Safeguards`                     TEXT          NULL,
    `RetentionPeriodTicks`           BIGINT        NOT NULL,
    `SecurityMeasures`               TEXT          NOT NULL,
    `CreatedAtUtc`                   DATETIME(6)   NOT NULL,
    `LastUpdatedAtUtc`               DATETIME(6)   NOT NULL,
    UNIQUE KEY `UQ_ProcessingActivities_RequestTypeName` (`RequestTypeName`)
);

CREATE INDEX `IX_ProcessingActivities_LawfulBasisValue` ON `ProcessingActivities` (`LawfulBasisValue`);
CREATE INDEX `IX_ProcessingActivities_CreatedAtUtc` ON `ProcessingActivities` (`CreatedAtUtc`);
