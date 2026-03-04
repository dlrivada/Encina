CREATE TABLE `BreachPhasedReports` (
    `Id`                VARCHAR(36)   NOT NULL PRIMARY KEY,
    `BreachId`          VARCHAR(36)   NOT NULL,
    `ReportNumber`      INT           NOT NULL,
    `Content`           TEXT          NOT NULL,
    `SubmittedAtUtc`    DATETIME(6)   NOT NULL,
    `SubmittedByUserId` VARCHAR(256)  NULL,
    INDEX `IX_BreachPhasedReports_BreachId` (`BreachId`),
    CONSTRAINT `FK_BreachPhasedReports_BreachRecords` FOREIGN KEY (`BreachId`) REFERENCES `BreachRecords`(`Id`)
);
