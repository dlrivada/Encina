-- =============================================
-- Create AuditLogs table for MySQL/MariaDB
-- For audit trail tracking
-- =============================================

CREATE TABLE IF NOT EXISTS `AuditLogs`
(
    `Id` CHAR(36) NOT NULL,
    `EntityType` VARCHAR(256) NOT NULL,
    `EntityId` VARCHAR(256) NOT NULL,
    `Action` INT NOT NULL,
    `UserId` VARCHAR(256) NULL,
    `TimestampUtc` DATETIME(6) NOT NULL,
    `OldValues` LONGTEXT NULL,
    `NewValues` LONGTEXT NULL,
    `CorrelationId` VARCHAR(256) NULL,

    PRIMARY KEY (`Id`),

    INDEX `IX_AuditLogs_Entity` (`EntityType`, `EntityId`),
    INDEX `IX_AuditLogs_Timestamp` (`TimestampUtc`),
    INDEX `IX_AuditLogs_UserId` (`UserId`),
    INDEX `IX_AuditLogs_CorrelationId` (`CorrelationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
