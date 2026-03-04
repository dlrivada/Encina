CREATE TABLE `BreachAuditEntries` (
    `Id`                VARCHAR(36)   NOT NULL PRIMARY KEY,
    `BreachId`          VARCHAR(36)   NOT NULL,
    `Action`            VARCHAR(256)  NOT NULL,
    `Detail`            TEXT          NULL,
    `PerformedByUserId` VARCHAR(256)  NULL,
    `OccurredAtUtc`     DATETIME(6)   NOT NULL,
    INDEX `IX_BreachAuditEntries_BreachId` (`BreachId`)
);
