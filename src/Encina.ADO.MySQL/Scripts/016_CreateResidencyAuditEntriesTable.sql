CREATE TABLE `ResidencyAuditEntries` (
    `Id`           VARCHAR(36)  NOT NULL PRIMARY KEY,
    `EntityId`     VARCHAR(256) NULL,
    `DataCategory` VARCHAR(256) NOT NULL,
    `SourceRegion` VARCHAR(32)  NOT NULL,
    `TargetRegion` VARCHAR(32)  NULL,
    `ActionValue`  INT          NOT NULL,
    `OutcomeValue` INT          NOT NULL,
    `LegalBasis`   VARCHAR(256) NULL,
    `RequestType`  VARCHAR(512) NULL,
    `UserId`       VARCHAR(256) NULL,
    `TimestampUtc` DATETIME(6)  NOT NULL,
    `Details`      TEXT         NULL,
    INDEX `IX_ResidencyAuditEntries_EntityId` (`EntityId`),
    INDEX `IX_ResidencyAuditEntries_TimestampUtc` (`TimestampUtc`),
    INDEX `IX_ResidencyAuditEntries_OutcomeValue` (`OutcomeValue`)
);
