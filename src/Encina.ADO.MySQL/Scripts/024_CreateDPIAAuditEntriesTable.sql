CREATE TABLE `DPIAAuditEntries` (
    `Id`            VARCHAR(36)   NOT NULL PRIMARY KEY,
    `AssessmentId`  VARCHAR(36)   NOT NULL,
    `Action`        VARCHAR(256)  NOT NULL,
    `PerformedBy`   VARCHAR(256)  NULL,
    `OccurredAtUtc` DATETIME(6)   NOT NULL,
    `Details`       TEXT          NULL,
    `TenantId`      VARCHAR(256)  NULL,
    `ModuleId`      VARCHAR(256)  NULL,
    INDEX `IX_DPIAAuditEntries_AssessmentId` (`AssessmentId`)
);
