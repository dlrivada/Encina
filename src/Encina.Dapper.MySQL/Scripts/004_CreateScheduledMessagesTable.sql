-- =============================================
-- Create ScheduledMessages table
-- For delayed and recurring command execution
-- =============================================

CREATE TABLE IF NOT EXISTS `ScheduledMessages`
(
    `Id` CHAR(36) NOT NULL PRIMARY KEY,
    `RequestType` VARCHAR(500) NOT NULL,
    `Content` LONGTEXT NOT NULL,
    `ScheduledAtUtc` DATETIME(6) NOT NULL,
    `CreatedAtUtc` DATETIME(6) NOT NULL,
    `ProcessedAtUtc` DATETIME(6) NULL,
    `LastExecutedAtUtc` DATETIME(6) NULL,
    `ErrorMessage` LONGTEXT NULL,
    `RetryCount` INT NOT NULL DEFAULT 0,
    `NextRetryAtUtc` DATETIME(6) NULL,
    `CorrelationId` VARCHAR(256) NULL,
    `Metadata` TEXT NULL,
    `IsRecurring` TINYINT(1) NOT NULL DEFAULT 0,
    `CronExpression` VARCHAR(100) NULL,

    INDEX `IX_ScheduledMessages_ScheduledAt_Processed` (`ScheduledAtUtc`, `ProcessedAtUtc`, `RetryCount`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
