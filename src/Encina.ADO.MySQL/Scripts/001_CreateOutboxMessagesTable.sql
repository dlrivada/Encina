-- =============================================
-- Create OutboxMessages table
-- For reliable event publishing (at-least-once delivery)
-- =============================================

CREATE TABLE IF NOT EXISTS `OutboxMessages` (
    `Id` CHAR(36) NOT NULL,
    `NotificationType` VARCHAR(500) NOT NULL,
    `Content` LONGTEXT NOT NULL,
    `CreatedAtUtc` DATETIME(6) NOT NULL,
    `ProcessedAtUtc` DATETIME(6) NULL,
    `ErrorMessage` LONGTEXT NULL,
    `RetryCount` INT NOT NULL DEFAULT 0,
    `NextRetryAtUtc` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_OutboxMessages_ProcessedAt_RetryCount` (`ProcessedAtUtc`, `RetryCount`, `NextRetryAtUtc`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
