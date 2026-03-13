CREATE TABLE `ProcessorAgreementAuditEntries` (
    `Id`                VARCHAR(256)  NOT NULL PRIMARY KEY,
    `ProcessorId`       VARCHAR(256)  NOT NULL,
    `DPAId`             VARCHAR(256)  NULL,
    `Action`            VARCHAR(256)  NOT NULL,
    `Detail`            TEXT          NULL,
    `PerformedByUserId` VARCHAR(256)  NULL,
    `OccurredAtUtc`     DATETIME(6)   NOT NULL,
    `TenantId`          VARCHAR(256)  NULL,
    `ModuleId`          VARCHAR(256)  NULL,
    INDEX `IX_ProcessorAgreementAuditEntries_ProcessorId` (`ProcessorId`)
);
